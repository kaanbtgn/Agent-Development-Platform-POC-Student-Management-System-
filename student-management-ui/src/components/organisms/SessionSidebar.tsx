import { useEffect, useState } from 'react';
import { useSessionStore } from '@/store/sessionStore';
import { useChatStore } from '@/store/chatStore';
import { ConfirmationDialog } from '@/components/molecules/ConfirmationDialog';
import { Spinner } from '@/components/atoms/Spinner';

interface SessionSidebarProps {
  onSessionChange: () => void;
}

export function SessionSidebar({ onSessionChange }: SessionSidebarProps) {
  const {
    sessions,
    currentSessionId,
    loading,
    fetchSessions,
    createSession,
    switchSession,
    deleteSession,
  } = useSessionStore();
  const { loadSession, clearHistory } = useChatStore();

  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [switching, setSwitching] = useState(false);

  useEffect(() => {
    const init = async () => {
      await fetchSessions();

      // If no session persisted in localStorage, create one automatically
      const { currentSessionId, sessions, createSession: create } = useSessionStore.getState();
      if (!currentSessionId && sessions.length === 0) {
        await create();
        onSessionChange();
      } else if (!currentSessionId && sessions.length > 0) {
        // Restore to most recent
        const messages = await switchSession(sessions[0].sessionId);
        loadSession(messages);
        onSessionChange();
      }
    };
    init();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleNew = async () => {
    clearHistory();
    const sessionId = await createSession();
    onSessionChange();
    return sessionId;
  };

  const handleSwitch = async (sessionId: string) => {
    if (sessionId === currentSessionId || switching) return;
    setSwitching(true);
    try {
      const messages = await switchSession(sessionId);
      loadSession(messages);
      onSessionChange();
    } finally {
      setSwitching(false);
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingId) return;
    const wasActive = deletingId === currentSessionId;
    await deleteSession(deletingId);
    setDeletingId(null);

    if (wasActive) {
      const next = useSessionStore.getState().sessions[0];
      if (next) {
        const messages = await switchSession(next.sessionId);
        loadSession(messages);
      } else {
        clearHistory();
        await createSession();
      }
      onSessionChange();
    }
  };

  return (
    <div className="flex w-56 shrink-0 flex-col" style={{ background: 'rgba(20,16,50,0.6)', borderRight: '1px solid rgba(99,102,241,0.15)', backdropFilter: 'blur(12px)' }}>
      <div className="p-3" style={{ borderBottom: '1px solid rgba(99,102,241,0.12)' }}>
        <button
          onClick={handleNew}
          className="flex w-full items-center justify-center gap-2 rounded-xl px-3 py-2.5 text-sm font-semibold text-white transition-all duration-200 hover:-translate-y-0.5 active:scale-95 shadow-lg shadow-indigo-500/20"
          style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}
        >
          <span className="text-base leading-none">+</span>
          Yeni Sohbet
        </button>
      </div>

      <div className="flex flex-1 flex-col overflow-y-auto py-2">
        {loading && sessions.length === 0 ? (
          <div className="flex justify-center pt-6">
            <Spinner size="sm" />
          </div>
        ) : sessions.length === 0 ? (
          <p className="px-3 pt-4 text-center text-xs text-indigo-300/40">
            Henüz sohbet yok.
          </p>
        ) : (
          sessions.map((session) => {
            const isActive = session.sessionId === currentSessionId;
            return (
              <div
                key={session.sessionId}
                className={`group relative flex cursor-pointer items-center gap-1 rounded-xl mx-2 px-3 py-2.5 my-0.5 transition-all duration-150 ${
                  isActive
                    ? 'text-white'
                    : 'text-indigo-200/50 hover:text-indigo-100/80'
                }`}
                style={isActive ? {
                  background: 'linear-gradient(135deg, rgba(99,102,241,0.25), rgba(139,92,246,0.15))',
                  borderLeft: '2px solid #818cf8',
                } : { borderLeft: '2px solid transparent' }}
                onClick={() => handleSwitch(session.sessionId)}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => e.key === 'Enter' && handleSwitch(session.sessionId)}
              >
                <div className="min-w-0 flex-1">
                  <p className="truncate text-xs font-medium leading-snug">
                    {session.title}
                  </p>
                  <p className="text-[10px] text-indigo-300/40">
                    {session.messageCount} mesaj
                  </p>
                </div>

                <button
                  className="ml-1 shrink-0 rounded p-0.5 text-indigo-300/20 opacity-0 transition-opacity hover:text-red-400 group-hover:opacity-100"
                  onClick={(e) => {
                    e.stopPropagation();
                    setDeletingId(session.sessionId);
                  }}
                  title="Sil"
                  aria-label="Sohbeti sil"
                >
                  🗑
                </button>
              </div>
            );
          })
        )}
      </div>

      {deletingId && (
        <ConfirmationDialog
          title="Sohbeti Sil"
          description="Bu sohbet kalıcı olarak silinecek. Devam etmek istiyor musunuz?"
          confirmLabel="Sil"
          cancelLabel="İptal"
          onConfirm={handleDeleteConfirm}
          onCancel={() => setDeletingId(null)}
        />
      )}
    </div>
  );
}
