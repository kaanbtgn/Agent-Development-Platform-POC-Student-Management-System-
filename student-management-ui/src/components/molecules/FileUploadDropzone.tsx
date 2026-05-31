import { useCallback, useRef } from 'react';
import { useOcrStore } from '@/store/ocrStore';

const MAX_SIZE_BYTES = 10 * 1024 * 1024;
const ALLOWED_TYPES = [
  // Görseller
  'image/jpeg', 'image/png', 'image/webp', 'image/gif',
  // PDF
  'application/pdf',
  // Düz metin
  'text/plain',
  // Microsoft Office
  'application/msword',                                                          // .doc
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',    // .docx
  'application/vnd.ms-excel',                                                   // .xls
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',          // .xlsx
  'application/vnd.ms-powerpoint',                                              // .ppt
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',  // .pptx
];

interface FileUploadDropzoneProps {
  onFileSelected?: (file: File) => void;
  onError?: (msg: string) => void;
}

export function FileUploadDropzone({ onFileSelected, onError }: FileUploadDropzoneProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const { reset: resetOcr } = useOcrStore();

  const handleFile = useCallback(
    (file: File) => {
      if (!ALLOWED_TYPES.includes(file.type)) {
        onError?.('Desteklenmeyen dosya türü. Lütfen resim veya PDF yükleyin.');
        return;
      }
      if (file.size > MAX_SIZE_BYTES) {
        onError?.('Dosya boyutu 10 MB sınırını aşıyor.');
        return;
      }
      resetOcr();
      onFileSelected?.(file);
    },
    [resetOcr, onFileSelected, onError]
  );

  return (
    <>
      <button
        type="button"
        title="Dosya ekle"
        className="flex h-10 w-10 items-center justify-center rounded-xl transition-all hover:scale-105 active:scale-95"
        style={{ background: 'rgba(255,255,255,0.1)', border: '1px solid rgba(255,255,255,0.15)' }}
        onClick={() => inputRef.current?.click()}
      >
        <svg className="h-5 w-5 text-white/70" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
            d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" />
        </svg>
      </button>
      <input
        ref={inputRef}
        type="file"
        className="hidden"
        accept="image/*,application/pdf,text/plain,.doc,.docx,.xls,.xlsx,.ppt,.pptx"
        onChange={(e) => { const f = e.target.files?.[0]; if (f) handleFile(f); e.target.value = ''; }}
      />
    </>
  );
}
