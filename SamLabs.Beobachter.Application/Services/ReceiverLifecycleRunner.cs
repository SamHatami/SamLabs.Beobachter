using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

internal static class ReceiverLifecycleRunner
{
    public static async ValueTask<IReadOnlyList<ReceiverStartupResult>> StartReceiversIndependentlyAsync(
        IReadOnlyList<ILogReceiver> receivers,
        ICollection<ILogReceiver> startedReceivers,
        ChannelWriter<LogEntry> writer,
        CancellationToken receiverToken,
        CancellationToken cancellationToken,
        ReceiverRuntimeStateRegistry runtimeStateRegistry)
    {
        List<ReceiverStartupResult> startupResults = new(receivers.Count);

        foreach (ILogReceiver receiver in receivers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeStateRegistry.SetState(receiver, ReceiverRunState.Starting);

            try
            {
                await receiver.StartAsync(writer, receiverToken).ConfigureAwait(false);
                startedReceivers.Add(receiver);
                runtimeStateRegistry.SetState(receiver, ReceiverRunState.Running);
                startupResults.Add(new ReceiverStartupResult
                {
                    ReceiverId = receiver.Id,
                    DisplayName = receiver.DisplayName,
                    Succeeded = true
                });
            }
            catch (OperationCanceledException) when (receiverToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                try
                {
                    await receiver.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Startup failed; receiver cleanup is best effort.
                }

                string errorMessage = GetStartupErrorMessage(ex);
                startupResults.Add(new ReceiverStartupResult
                {
                    ReceiverId = receiver.Id,
                    DisplayName = receiver.DisplayName,
                    Succeeded = false,
                    ErrorMessage = errorMessage
                });
                runtimeStateRegistry.SetState(receiver, ReceiverRunState.Faulted, errorMessage);
            }
        }

        return startupResults;
    }

    private static string GetStartupErrorMessage(Exception exception)
    {
        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            return exception.Message;
        }

        return exception.GetType().Name;
    }
}
