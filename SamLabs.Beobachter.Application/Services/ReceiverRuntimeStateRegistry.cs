using System;
using System.Collections.Generic;
using System.Linq;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

internal sealed class ReceiverRuntimeStateRegistry
{
    private readonly Dictionary<string, ReceiverRuntimeState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public IReadOnlyList<ReceiverRuntimeState> Snapshot()
    {
        lock (_gate)
        {
            return _states.Values
                .OrderBy(static x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static x => x.ReceiverId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _states.Clear();
        }
    }

    public void ReplaceEntries(IReadOnlyList<ILogReceiver> receivers)
    {
        HashSet<string> receiverIds = new(receivers.Select(static x => x.Id), StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            string[] staleIds = _states.Keys
                .Where(id => !receiverIds.Contains(id))
                .ToArray();

            foreach (string staleId in staleIds)
            {
                _states.Remove(staleId);
            }

            foreach (ILogReceiver receiver in receivers)
            {
                if (_states.ContainsKey(receiver.Id))
                {
                    continue;
                }

                _states[receiver.Id] = new ReceiverRuntimeState
                {
                    ReceiverId = receiver.Id,
                    DisplayName = receiver.DisplayName,
                    State = ReceiverRunState.Stopped
                };
            }
        }
    }

    public void MarkStopped(IEnumerable<ILogReceiver> receivers)
    {
        foreach (ILogReceiver receiver in receivers)
        {
            SetState(receiver, ReceiverRunState.Stopped);
        }
    }

    public void SetState(
        ILogReceiver receiver,
        ReceiverRunState state,
        string? error = null)
    {
        lock (_gate)
        {
            if (!_states.TryGetValue(receiver.Id, out ReceiverRuntimeState? current))
            {
                current = new ReceiverRuntimeState
                {
                    ReceiverId = receiver.Id,
                    DisplayName = receiver.DisplayName
                };
            }

            _states[receiver.Id] = current with
            {
                DisplayName = receiver.DisplayName,
                State = state,
                LastError = error ?? (state == ReceiverRunState.Faulted ? current.LastError : string.Empty)
            };
        }
    }
}
