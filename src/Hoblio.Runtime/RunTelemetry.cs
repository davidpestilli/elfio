namespace Hoblio.Runtime;

sealed class RunTelemetry
{
    public int LogIndex { get; private set; }
    public int StepIndex { get; private set; }

    public int CurrentStepIndex => StepIndex;

    public int NextLogIndex() => ++LogIndex;
    public int NextStepIndex() => ++StepIndex;
}
