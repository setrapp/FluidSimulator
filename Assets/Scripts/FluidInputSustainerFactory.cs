using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FluidInputSustainerFactory : MonoBehaviour
{
    class RepeatData
    {
        public float delay;
        public float waitRemaining;
        public float dampening;

        public RepeatData(float delay, float dampening)
        {
            this.delay = delay;
            this.waitRemaining = 0;
            this.dampening = dampening;
        }
    }

    public static FluidInputSustainerFactory Instance;

    [SerializeField]
    List<FluidInputSustainer> attacking;
    [SerializeField]
    List<FluidInputSustainer> sustaining;
    [SerializeField]
    List<FluidInputSustainer> decaying;
    [SerializeField]
    List<FluidInputSustainer> releasing;
    [SerializeField]
    List<FluidInputSustainer> waiting;
    Dictionary<FluidInputSustainer, RepeatData> repeats;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple FluidInputSustainerFactory instances exist. Remove all but one.");
        }

        repeats = new Dictionary<FluidInputSustainer, RepeatData>();
    }

    public FluidInputSustainer Create(FluidCollider fluidCollider, Vector3 center,
        float attackDensityChangeRate, float attackDensityChangeRadius, Vector3 attackForceRate, float attackForceRadius,
        float attackRelativeSustain, float attackRelativeRelease, float sustainDuration, float decayDuration,
        float repeatDelay = -1, float repeatDampening = 0)
    {
        float sustainDensityChangeRate = attackDensityChangeRate * attackRelativeSustain;
        float sustainDensityChangeRadius = attackDensityChangeRadius * attackRelativeSustain;
        Vector3 sustainForceRate = attackForceRate * attackRelativeSustain;
        float sustainForceRadius = attackForceRadius * attackRelativeSustain;

        float releaseDensityChangeRate = attackDensityChangeRate * attackRelativeRelease;
        float releaseDensityChangeRadius = attackDensityChangeRadius * attackRelativeRelease;
        Vector3 releaseForceRate = attackForceRate * attackRelativeRelease;
        float releaseForceRadius = attackForceRadius * attackRelativeRelease;

        return Create(fluidCollider, center,
            attackDensityChangeRate, attackDensityChangeRadius, attackForceRate, attackForceRadius,
            sustainDensityChangeRate, sustainDensityChangeRadius, sustainForceRate, sustainForceRadius,
            releaseDensityChangeRate, releaseDensityChangeRadius, releaseForceRate, releaseForceRadius,
            sustainDuration, decayDuration);
    }

    public FluidInputSustainer Create(FluidCollider fluidCollider, Vector3 center,
        float attackDensityChangeRate, float attackDensityChangeRadius, Vector3 attackForceRate, float attackForceRadius,
        float sustainDensityChangeRate, float sustainDensityChangeRadius, Vector3 sustainForceRate, float sustainForceRadius,
        float releaseDensityChangeRate, float releaseDensityChangeRadius, Vector3 releaseForceRate, float releaseForceRadius,
        float sustainDuration, float decayDuration,
        float repeatDelay = -1, float repeatDampening = 0)
    {
        FluidInputSustainer sustainer = new FluidInputSustainer(fluidCollider, center,
            attackDensityChangeRate, attackDensityChangeRadius, attackForceRate, attackForceRadius,
            sustainDensityChangeRate, sustainDensityChangeRadius, sustainForceRate, sustainForceRadius,
            releaseDensityChangeRate, releaseDensityChangeRadius, releaseForceRate, releaseForceRadius,
            sustainDuration, decayDuration);

        if (repeatDelay >= 0 && repeatDampening > 0)
        {
            repeats.Add(sustainer, new RepeatData(repeatDelay, Mathf.Clamp01(repeatDampening)));
        }

        sustainer.Attack();
        waiting.Add(sustainer);

        return sustainer;
    }

    void Update()
    {
        while (releasing.Count > 0)
        {
            FluidInputSustainer releasee = releasing[0];
            releasee.Release();
            if (repeats.ContainsKey(releasee))
            {
                RepeatData repeat = repeats[releasee];
                releasee.dampening += repeat.dampening;
                if (releasee.dampening >= 1)
                {
                    repeats.Remove(releasee);
                }
                else if (repeat.waitRemaining > 0)
                {
                    repeat.waitRemaining = repeat.delay;
                    waiting.Add(releasee);
                }
            }
            releasing.RemoveAt(0);
        }

        for (int i = 0; i < decaying.Count; i++)
        {
            bool continueDecay = decaying[i].Decay();
            if (!continueDecay)
            {
                releasing.Add(decaying[i]);
                decaying.RemoveAt(i);
            }
        }

        for (int i = 0; i < sustaining.Count; i++)
        {
            bool continueSustain = sustaining[i].Sustain();
            if (!continueSustain)
            {
                decaying.Add(sustaining[i]);
                sustaining.RemoveAt(i);
            }
        }

        while (attacking.Count > 0)
        {
            attacking[0].Attack();
            attacking.RemoveAt(0);
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < waiting.Count; i++)
        {
            FluidInputSustainer waitee = waiting[i];
            if (!waitee.IsActive())
            {
                if (repeats.ContainsKey(waitee))
                {
                    // Inactive Sustainers that have repeat data get added to Attack queue when their delay is finished.
                    if (repeats[waitee].delay <= 0)
                    {
                        attacking.Add(waitee);
                        repeats[waitee].waitRemaining = repeats[waitee].delay;
                        waiting.RemoveAt(i);
                    }
                    else
                    {
                        repeats[waitee].delay -= Time.deltaTime;
                    }
                }
            }
            else
            {
                // Active Sustainers that are waiting have already attacked, so prepare them to sustain.
                sustaining.Add(waitee);
                waiting.RemoveAt(i);
            }
        }
    }
}
