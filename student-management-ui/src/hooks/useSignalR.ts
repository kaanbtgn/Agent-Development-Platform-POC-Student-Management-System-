import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useChatStore } from '@/store/chatStore';
import { useOcrStore } from '@/store/ocrStore';
import type { OcrProgress } from '@/types/agent.types';

export function useSignalR(sessionId: string) {
  const connection = useRef<HubConnection | null>(null);

  useEffect(() => {
    if (!sessionId) return;

    const hub = new HubConnectionBuilder()
      .withUrl(import.meta.env.VITE_SIGNALR_HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.current = hub;

    // Zustand actions are stable references — access via getState() inside handlers
    // so they don't need to be in the dependency array and won't cause reconnects
    // on every store state change (e.g. new message, isThinking flip).
    hub.on('AgentThinking', () => useChatStore.getState().setThinking(true));
    hub.on('MessageReceived', () => useChatStore.getState().setThinking(false));
    hub.on('AgentResponseCompleted', (payload: { response: string }) =>
      useChatStore.getState().setAssistantMessage(payload.response)
    );
    hub.on('AgentError', (payload: { error: string }) => useChatStore.getState().setError(payload.error));
    hub.on('AgentTokenReceived', (payload: { token: string }) => useChatStore.getState().appendToken(payload.token));
    hub.on('OcrProgressUpdated', (payload: OcrProgress) => useOcrStore.getState().setProgress(payload));
    hub.on('OcrCompleted', (payload: { result: string }) => useOcrStore.getState().setCompleted(payload.result));
    hub.on('OcrFailed', (payload: { error: string }) => useOcrStore.getState().setFailed(payload.error));

    hub
      .start()
      .then(() => hub.invoke('JoinSession', sessionId))
      .catch((err: Error) => console.error('[SignalR] Connection error:', err));

    return () => {
      // Only send LeaveSession when fully connected.
      // Calling invoke() while still in "Connecting" state (e.g. React StrictMode
      // double-invoke or rapid sessionId change) causes "stopped during negotiation".
      if (hub.state === HubConnectionState.Connected) {
        hub.invoke('LeaveSession', sessionId)
          .finally(() => hub.stop().catch(() => {}));
      } else {
        hub.stop().catch(() => {});
      }
    };
  }, [sessionId]); // Only reconnect when the session changes; store actions are stable
}
