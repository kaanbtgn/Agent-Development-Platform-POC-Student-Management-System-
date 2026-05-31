import { useOcrStore } from '@/store/ocrStore';

export function OcrProgressBar() {
  const { isActive, progress, error } = useOcrStore();

  if (!isActive && !error) return null;

  return (
    <div className="rounded-lg border border-white/10 p-4"
      style={{ background: 'rgba(255,255,255,0.06)' }}>
      <div className="mb-2 flex items-center justify-between text-sm">
        <span className="font-medium text-white/80">
          {error ? 'OCR Başarısız' : (progress?.step ?? 'Başlatılıyor...')}
        </span>
        {!error && (
          <span className="text-white/40">{progress?.progressPercent ?? 0}%</span>
        )}
      </div>
      {error ? (
        <p className="text-sm text-red-400">{error}</p>
      ) : (
        <div className="h-2 w-full overflow-hidden rounded-full bg-white/10">
          <div
            className="h-full rounded-full bg-indigo-500 transition-all duration-300"
            style={{ width: `${progress?.progressPercent ?? 0}%` }}
          />
        </div>
      )}
    </div>
  );
}
