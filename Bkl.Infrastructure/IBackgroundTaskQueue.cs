using System.Threading;
using System.Threading.Tasks;

 
public interface IBackgroundTaskQueue<T>: IBackgroundQueueQueue
{
	ValueTask EnqueueAsync(T request);

	ValueTask<T> DequeueAsync(
		CancellationToken cancellationToken);
}


public interface IBackgroundQueueQueue
{
	ValueTask EnqueueAsync(object request);
}


