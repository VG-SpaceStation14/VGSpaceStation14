using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Content.Server._VG.SimpleSkills;

public sealed class AsyncSkillCheck
{
    private readonly SimpleSkillSystem _skillSystem;
    private readonly ConcurrentQueue<SkillCheckRequest> _pendingChecks = new();
    private bool _isProcessing;
    private readonly ISawmill _sawmill;

    private struct SkillCheckRequest
    {
        public EntityUid User;
        public string RequiredSkill;
        public Action<bool> Callback;
        public DateTime EnqueueTime;
    }

    public AsyncSkillCheck(SimpleSkillSystem skillSystem, ISawmill sawmill)
    {
        _skillSystem = skillSystem;
        _sawmill = sawmill;
    }

    public void CheckSkillAsync(EntityUid user, string skillId, Action<bool> callback)
    {
        // Быстрая проверка — синхронно (для кэшированных навыков)
        if (_skillSystem.GetSkillPrototype(skillId) != null)
        {
            callback(_skillSystem.HasSkill(user, skillId));
            return;
        }

        // Сложная проверка — в очередь
        _pendingChecks.Enqueue(new SkillCheckRequest
        {
            User = user,
            RequiredSkill = skillId,
            Callback = callback,
            EnqueueTime = DateTime.UtcNow
        });

        if (!_isProcessing)
        {
            ProcessNextCheck();
        }
    }

    private async void ProcessNextCheck()
    {
        if (_pendingChecks.IsEmpty)
        {
            _isProcessing = false;
            return;
        }

        _isProcessing = true;

        if (_pendingChecks.TryDequeue(out var request))
        {
            try
            {
                var result = await Task.Run(() =>
                {
                    return _skillSystem.HasSkill(request.User, request.RequiredSkill);
                });

                var elapsed = DateTime.UtcNow - request.EnqueueTime;
                if (elapsed.TotalMilliseconds > 100)
                {
                    _sawmill.Debug($"Async skill check took {elapsed.TotalMilliseconds}ms for {request.RequiredSkill}");
                }

                request.Callback(result);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Async skill check failed: {e.Message}");
                request.Callback(false);
            }
        }

        ProcessNextCheck();
    }

    public int GetPendingCount() => _pendingChecks.Count;
}