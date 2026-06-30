import { create } from 'zustand';
import type { ChatMessage } from '@/types/agent.types';
import type { SessionMessage } from '@/types/session.types';

interface ChatState {
  messages: ChatMessage[];
  isThinking: boolean;
  isStreaming: boolean;
  currentStreamingContent: string;
  setThinking: (value: boolean) => void;
  appendToken: (token: string) => void;
  setAssistantMessage: (content: string) => void;
  addUserMessage: (content: string) => void;
  setError: (error: string) => void;
  clearHistory: () => void;
  loadSession: (sessionMessages: SessionMessage[]) => void;
}

export const useChatStore = create<ChatState>((set) => ({
  messages: [],
  isThinking: false,
  isStreaming: false,
  currentStreamingContent: '',

  setThinking: (value) => set({ isThinking: value }),

  appendToken: (token) =>
    set((state) => ({
      isThinking: false, // İlk token gelince "düşünüyor" göstergesini kapat
      isStreaming: true,
      currentStreamingContent: state.currentStreamingContent + token,
    })),

  setAssistantMessage: (content) =>
    set((state) => ({
      isThinking: false,
      isStreaming: false,
      currentStreamingContent: '',
      messages: [
        ...state.messages,
        {
          id: crypto.randomUUID(),
          role: 'assistant',
          content,
          timestamp: new Date().toISOString(),
        },
      ],
    })),

  addUserMessage: (content) =>
    set((state) => ({
      messages: [
        ...state.messages,
        {
          id: crypto.randomUUID(),
          role: 'user',
          content,
          timestamp: new Date().toISOString(),
        },
      ],
    })),

  setError: (error) =>
    set((state) => ({
      isThinking: false,
      isStreaming: false,
      messages: [
        ...state.messages,
        {
          id: crypto.randomUUID(),
          role: 'assistant',
          content: `Hata: ${error}`,
          timestamp: new Date().toISOString(),
        },
      ],
    })),

  clearHistory: () =>
    set({ messages: [], isThinking: false, isStreaming: false, currentStreamingContent: '' }),

  loadSession: (sessionMessages) =>
    set({
      messages: sessionMessages.map((m) => ({
        id: crypto.randomUUID(),
        role: m.role,
        content: m.content,
        timestamp: m.timestamp,
      })),
      isThinking: false,
      isStreaming: false,
      currentStreamingContent: '',
    }),
}));
