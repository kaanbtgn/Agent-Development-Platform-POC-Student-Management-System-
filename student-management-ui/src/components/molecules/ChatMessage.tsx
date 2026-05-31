import { cn } from '@/lib/utils';
import axiosInstance from '@/api/axiosInstance';
import type { ChatMessage as ChatMessageType } from '@/types/agent.types';

interface ChatMessageProps {
  message: ChatMessageType;
}

// Matches /api/docs/{uuid} anywhere in text
const DOC_URL_RE = /(\/api\/docs\/[0-9a-f-]{36})/gi;

function parseMessageParts(content: string): Array<{ type: 'text'; value: string } | { type: 'download'; url: string }> {
  const parts: Array<{ type: 'text'; value: string } | { type: 'download'; url: string }> = [];
  let lastIndex = 0;
  let match: RegExpExecArray | null;
  DOC_URL_RE.lastIndex = 0;

  while ((match = DOC_URL_RE.exec(content)) !== null) {
    if (match.index > lastIndex) {
      parts.push({ type: 'text', value: content.slice(lastIndex, match.index) });
    }
    parts.push({ type: 'download', url: match[1] });
    lastIndex = match.index + match[1].length;
  }

  if (lastIndex < content.length) {
    parts.push({ type: 'text', value: content.slice(lastIndex) });
  }

  return parts.length > 0 ? parts : [{ type: 'text', value: content }];
}

async function triggerDownload(url: string) {
  const response = await axiosInstance.get(url, { responseType: 'blob' });
  const disposition = response.headers['content-disposition'] as string | undefined;
  const nameMatch = disposition?.match(/filename\*?=(?:UTF-8'')?["']?([^"';\r\n]+)/i);
  const fileName = nameMatch?.[1] ? decodeURIComponent(nameMatch[1]) : 'belge';
  const blob = response.data as Blob;
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(objectUrl);
}

export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === 'user';
  const parts = isUser ? null : parseMessageParts(message.content);

  return (
    <div className={cn('flex gap-3 animate-slide-in-up', isUser ? 'justify-end' : 'justify-start')}>
      {!isUser && (
        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-bold text-white shadow-lg shadow-indigo-500/30"
          style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}
        >
          AI
        </div>
      )}
      <div
        className={cn(
          'max-w-[75%] rounded-2xl px-4 py-2.5 text-sm leading-relaxed shadow-sm',
          isUser
            ? 'rounded-tr-sm text-white shadow-indigo-500/20'
            : 'rounded-tl-sm border border-white/60 bg-white/80 text-gray-800 backdrop-blur-sm'
        )}
        style={isUser ? { background: 'linear-gradient(135deg, #6366f1, #7c3aed)' } : {}}
      >
        {message.isStreaming ? (
          <span>
            {message.content}
            <span className="ml-0.5 inline-block animate-pulse text-indigo-400">▌</span>
          </span>
        ) : isUser || !parts ? (
          message.content
        ) : (
          parts.map((part, i) =>
            part.type === 'text' ? (
              <span key={i}>{part.value}</span>
            ) : (
              <button
                key={i}
                onClick={() => triggerDownload(part.url)}
                className="mx-1 inline-flex items-center gap-1.5 rounded-lg px-3 py-1 text-xs font-semibold text-white hover:-translate-y-0.5 active:scale-95 transition-all shadow-md"
                style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}
              >
                ⬇ Belgeyi İndir
              </button>
            )
          )
        )}
      </div>
    </div>
  );
}

export function ThinkingIndicator() {
  return (
    <div className="flex justify-start gap-3 animate-fade-in">
      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-bold text-white shadow-lg shadow-indigo-500/30"
        style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}
      >
        AI
      </div>
      <div className="flex items-center gap-1.5 rounded-2xl rounded-tl-sm border border-white/60 bg-white/80 px-4 py-3 backdrop-blur-sm">
        {[0, 1, 2].map((i) => (
          <span
            key={i}
            className="h-2 w-2 rounded-full bg-indigo-400"
            style={{ animation: `bounce 1.2s ease-in-out ${i * 0.2}s infinite` }}
          />
        ))}
      </div>
    </div>
  );
}
