
public class ReminderSignal
{
    private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
    public Task WaitAsync(TimeSpan aTimeOut)
    {
        return Task.WhenAny(_tcs.Task, Task.Delay(aTimeOut));    
    }
    public void WakeUp()
    {
        var lOldTcs = _tcs;
        _tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );    
        lOldTcs.SetResult(true);
    }    
}