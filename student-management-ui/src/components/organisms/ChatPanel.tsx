import { useRef, useEffect } from 'react';
import { ChatMessage, ThinkingIndicator } from '@/components/molecules/ChatMessage';
import { useChatStore } from '@/store/chatStore';

export function ChatPanel() {
  const { messages, isThinking, isStreaming, currentStreamingContent } = useChatStore();
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isThinking, isStreaming, currentStreamingContent]);

  return (
    <div className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 py-4"
      style={{ background: 'transparent' }}
    >
      {messages.length === 0 && !isStreaming && (
        <div className="flex flex-1 flex-col items-center justify-center gap-3">
          <div className="flex h-16 w-16 items-center justify-center rounded-2xl text-3xl shadow-xl shadow-indigo-500/20"
            style={{ background: 'linear-gradient(135deg, rgba(99,102,241,0.3), rgba(139,92,246,0.2))', border: '1px solid rgba(99,102,241,0.3)' }}>
            💬
          </div>
          <p className="text-sm font-medium text-white/40">Bir mesaj yazarak sohbete başlayın.</p>
        </div>
      )}
      {messages.map((msg) => (
        <ChatMessage key={msg.id} message={msg} />
      ))}
      {/* Streaming sırasında token-by-token güncellenen canlı balon */}
      {isStreaming && currentStreamingContent && (
        <ChatMessage
          message={{
            id: '__streaming__',
            role: 'assistant',
            content: currentStreamingContent,
            timestamp: new Date().toISOString(),
            isStreaming: true,
          }}
        />
      )}
      {isThinking && !isStreaming && <ThinkingIndicator />}
      <div ref={bottomRef} />
    </div>
  );
}
