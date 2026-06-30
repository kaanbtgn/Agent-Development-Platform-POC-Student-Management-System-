import { useCallback, useEffect, useRef, useState } from 'react';
import { ChatPanel } from '@/components/organisms/ChatPanel';
import { SessionSidebar } from '@/components/organisms/SessionSidebar';
import { OcrProgressBar } from '@/components/molecules/OcrProgressBar';
import { FileUploadDropzone } from '@/components/molecules/FileUploadDropzone';
import { Spinner } from '@/components/atoms/Spinner';
import { useChatStore } from '@/store/chatStore';
import { useSessionStore } from '@/store/sessionStore';
import { useSignalR } from '@/hooks/useSignalR';
import { agentApi } from '@/api/agentApi';

const ALLOWED_TYPES = [
  'image/jpeg', 'image/png', 'image/webp', 'image/gif',
  'application/pdf',
  'text/plain',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'application/vnd.ms-powerpoint',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
];
const MAX_SIZE_BYTES = 10 * 1024 * 1024;

export function ChatPage() {
  const { addUserMessage, isThinking } = useChatStore();
  const { currentSessionId, fetchSessions: refreshSessions } = useSessionStore();
  const [input, setInput] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const [isDragOver, setIsDragOver] = useState(false);
  const dragCounterRef = useRef(0);
  const wasThinkingRef = useRef(false);

  useSignalR(currentSessionId);

  useEffect(() => {
    if (wasThinkingRef.current && !isThinking) {
      refreshSessions();
    }
    wasThinkingRef.current = isThinking;
  }, [isThinking, refreshSessions]);

  // Global drag handlers — tüm pencerede sürükleme algılanır
  useEffect(() => {
    const onDragEnter = (e: DragEvent) => {
      if (e.dataTransfer?.types.includes('Files')) {
        dragCounterRef.current += 1;
        setIsDragOver(true);
      }
    };
    const onDragLeave = () => {
      dragCounterRef.current -= 1;
      if (dragCounterRef.current <= 0) {
        dragCounterRef.current = 0;
        setIsDragOver(false);
      }
    };
    const onDragOver = (e: DragEvent) => e.preventDefault();
    const onDrop = (e: DragEvent) => {
      e.preventDefault();
      dragCounterRef.current = 0;
      setIsDragOver(false);
      const file = e.dataTransfer?.files[0];
      if (!file) return;
      if (!ALLOWED_TYPES.includes(file.type)) {
        setFileError('Desteklenmeyen dosya türü. Lütfen resim veya PDF yükleyin.');
        return;
      }
      if (file.size > MAX_SIZE_BYTES) {
        setFileError('Dosya boyutu 10 MB sınırını aşıyor.');
        return;
      }
      setFileError(null);
      setSelectedFile(file);
    };

    window.addEventListener('dragenter', onDragEnter);
    window.addEventListener('dragleave', onDragLeave);
    window.addEventListener('dragover', onDragOver);
    window.addEventListener('drop', onDrop);
    return () => {
      window.removeEventListener('dragenter', onDragEnter);
      window.removeEventListener('dragleave', onDragLeave);
      window.removeEventListener('dragover', onDragOver);
      window.removeEventListener('drop', onDrop);
    };
  }, []);

  const handleSessionChange = useCallback(() => {
    refreshSessions();
  }, [refreshSessions]);

  const handleSend = async () => {
    const message = input.trim();
    if (!message || sending) return;
    setInput('');
    const file = selectedFile;
    setSelectedFile(null);
    addUserMessage(message);
    setSending(true);
    useChatStore.getState().setThinking(true);
    try {
      let result;
      if (file) {
        result = await agentApi.chatWithDocument(message, file);
      } else {
        result = await agentApi.chat(message);
      }
      useChatStore.getState().setAssistantMessage(result?.reply ?? '');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Bir hata oluştu.';
      useChatStore.getState().setError(message);
    } finally {
      setSending(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="relative flex flex-1 overflow-hidden">
      {/* Sürükleme overlay */}
      {isDragOver && (
        <div className="pointer-events-none absolute inset-0 z-50 flex flex-col items-center justify-center gap-4"
          style={{ background: 'rgba(99,102,241,0.18)', backdropFilter: 'blur(6px)', border: '2px dashed rgba(139,92,246,0.7)' }}>
          <svg className="h-16 w-16 text-indigo-300 drop-shadow-lg" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
              d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" />
          </svg>
          <p className="text-2xl font-bold text-white drop-shadow">Dosyayı bırakın</p>
          <p className="text-sm text-indigo-200/80">PNG, JPG, PDF, TXT, Word, Excel, PowerPoint — maks. 10 MB</p>
        </div>
      )}

      <SessionSidebar onSessionChange={handleSessionChange} />

      <div className="flex flex-1 flex-col overflow-hidden">
        <header className="px-6 py-4 flex items-center gap-3" style={{ background: 'rgba(255,255,255,0.07)', borderBottom: '1px solid rgba(255,255,255,0.1)', backdropFilter: 'blur(20px)' }}>
          <div className="flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold text-white shadow-lg shadow-indigo-500/30"
            style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}>
            AI
          </div>
          <div>
            <h1 className="text-sm font-bold text-white">AI Asistan</h1>
            <p className="text-[10px] text-indigo-300/60">Öğrenci yönetim asistanı</p>
          </div>
        </header>

        <ChatPanel />

        <div className="px-4 py-3" style={{ background: 'rgba(255,255,255,0.07)', borderTop: '1px solid rgba(255,255,255,0.08)', backdropFilter: 'blur(20px)' }}>
          <OcrProgressBar />
          {selectedFile && (
            <div className="mb-2 flex items-center gap-2 rounded-xl px-3 py-1.5 text-sm text-indigo-200" style={{ background: 'rgba(99,102,241,0.15)', border: '1px solid rgba(99,102,241,0.3)' }}>
              <span>📎 {selectedFile.name}</span>
              <button
                className="ml-auto text-indigo-400/60 hover:text-indigo-300 transition-colors"
                onClick={() => setSelectedFile(null)}
              >
                ✕
              </button>
            </div>
          )}
          {fileError && (
            <p className="mb-2 text-xs text-red-400">{fileError}</p>
          )}
          <div className="flex items-end gap-2">
            <FileUploadDropzone
              onFileSelected={(f) => { setFileError(null); setSelectedFile(f); }}
              onError={setFileError}
            />
            <textarea
              rows={1}
              className="flex-1 resize-none rounded-xl px-4 py-2.5 text-sm text-white placeholder-white/30 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 transition-all"
              style={{ background: 'rgba(255,255,255,0.08)', border: '1px solid rgba(255,255,255,0.12)' }}
              placeholder="Mesajınızı yazın... (Enter ile gönder)"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              disabled={isThinking || sending}
            />
            <button
              className="flex h-10 w-10 items-center justify-center rounded-xl text-white transition-all hover:-translate-y-0.5 active:scale-95 disabled:opacity-40 shadow-lg shadow-indigo-500/30"
              style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}
              onClick={handleSend}
              disabled={!input.trim() || isThinking || sending}
            >
              {sending ? <Spinner size="sm" className="text-white" /> : '↑'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
