using UnityEngine;
using System.Collections;

[System.Serializable]
public class FluidInputSustainer
{
    [System.Serializable]
    struct Parameters
    {
        public float densityChangeRate;
        public float densityChangeRadius;
        public Vector3 forceRate;
        public float forceRadius;

        public Parameters(float densityChangeRate, float densityChangeRadius, Vector3 forceRate, float forceRadius)
        {
            this.densityChangeRate = densityChangeRate;
            this.densityChangeRadius = densityChangeRadius;
            this.forceRate = forceRate;
            this.forceRadius = forceRadius;
        }
    }

    FluidCollider fluidCollider;
    Vector3 center;
    Parameters attackParameters;
    Parameters sustainParameters;
    Parameters releaseParameters;
    float sustainDuration;
    float decayDuration;
    float timeRemaining;
    public float dampening;

    public FluidInputSustainer(FluidCollider fluidCollider, Vector3 center,
        float attackDensityChangeRate, float attackDensityChangeRadius, Vector3 attackForceRate, float attackForceRadius,
        float sustainDensityChangeRate, float sustainDensityChangeRadius, Vector3 sustainForceRate, float sustainForceRadius,
        float releaseDensityChangeRate, float releaseDensityChangeRadius, Vector3 releaseForceRate, float releaseForceRadius,
        float sustainDuration, float decayDuration)
    {
        this.fluidCollider = fluidCollider;
        this.center = center;
        attackParameters = new Parameters(attackDensityChangeRate, attackDensityChangeRadius, attackForceRate, attackForceRadius);
        sustainParameters = new Parameters(sustainDensityChangeRate, sustainDensityChangeRadius, sustainForceRate, sustainForceRadius);
        releaseParameters = new Parameters(releaseDensityChangeRate, releaseDensityChangeRadius, releaseForceRate, releaseForceRadius);
        this.sustainDuration = sustainDuration;
        this.decayDuration = decayDuration;
        timeRemaining = Mathf.Infinity;
        dampening = 0;
    }

    void ApplyToFluid(Parameters inputParameters)
    {
        float undampened = 1 - dampening;
        fluidCollider.AddExternal(center, inputParameters.densityChangeRate * undampened, inputParameters.densityChangeRadius * undampened, inputParameters.forceRate * undampened, inputParameters.forceRadius * undampened);
    }

    public void Attack()
    {
        ApplyToFluid(attackParameters);
        timeRemaining = sustainDuration;
    }
        // TODO Handle paused fluid somewhere
    public bool Sustain()
    {
        if (sustainDuration <= 0)
        {
            return false;
        }

        ApplyToFluid(sustainParameters);

        timeRemaining -= Time.deltaTime;
        bool continueSustain = timeRemaining > 0;
        if (!continueSustain)
        {
            timeRemaining = decayDuration;
        }

        return continueSustain;
    }

    public bool Decay()
    {
        if (decayDuration <= 0)
        {
            return false;
        }

        float decayPortion = 1 - (timeRemaining / decayDuration);
        Parameters decayParameters = new Parameters(
            Mathf.Lerp(sustainParameters.densityChangeRate, releaseParameters.densityChangeRate, decayPortion),
            Mathf.Lerp(sustainParameters.densityChangeRadius, releaseParameters.densityChangeRadius, decayPortion),
            Vector3.Lerp(sustainParameters.forceRate, releaseParameters.forceRate, decayPortion),
            Mathf.Lerp(sustainParameters.forceRadius, releaseParameters.forceRadius, decayPortion));


        ApplyToFluid(decayParameters);

        timeRemaining -= Time.deltaTime;
        bool continueDecay = timeRemaining > 0;
        if (!continueDecay)
        {
            timeRemaining = Mathf.Infinity;
        }

        return continueDecay;
    }

    public void Release()
    {
        ApplyToFluid(releaseParameters);
        timeRemaining = 0;
    }

    public bool IsActive()
    {
        return timeRemaining > 0;
    }
}
