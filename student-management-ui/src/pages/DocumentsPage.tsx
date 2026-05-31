import { useMemo } from 'react';
import { FileUploadDropzone } from '@/components/molecules/FileUploadDropzone';
import { OcrProgressBar } from '@/components/molecules/OcrProgressBar';
import { useOcrStore } from '@/store/ocrStore';
import { documentApi } from '@/api/documentApi';
import { useSignalR } from '@/hooks/useSignalR';

export function DocumentsPage() {
  const { resultJson, isActive, error } = useOcrStore();
  const sessionId = useMemo(() => crypto.randomUUID(), []);

  useSignalR(sessionId);

  const handleFileSelected = async (file: File) => {
    await documentApi.uploadAsync(file, sessionId);
  };

  return (
    <div className="mx-auto max-w-2xl p-8">
      <h1 className="mb-6 text-xl font-semibold text-white">Belge Yükle ve OCR</h1>

      <div className="mb-6 rounded-xl border-2 border-dashed border-white/20 p-6"
        style={{ background: 'rgba(255,255,255,0.04)' }}>
        <FileUploadDropzone onFileSelected={handleFileSelected} />
      </div>

      {isActive && (
        <div className="mb-6">
          <OcrProgressBar />
        </div>
      )}

      {error && (
        <p className="mb-4 text-sm text-red-400">{error}</p>
      )}

      {resultJson && (
        <div className="rounded-xl border border-white/10 p-4"
          style={{ background: 'rgba(255,255,255,0.06)' }}>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-indigo-200">OCR Sonucu</h2>
          </div>
          <pre className="overflow-x-auto text-xs text-white/60 whitespace-pre-wrap">
            {typeof resultJson === 'string' ? resultJson : JSON.stringify(resultJson, null, 2)}
          </pre>
        </div>
      )}
    </div>
  );
}
