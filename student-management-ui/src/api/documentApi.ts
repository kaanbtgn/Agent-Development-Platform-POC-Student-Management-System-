import axiosInstance from './axiosInstance';
import type { UploadAsyncResponse } from '@/types/agent.types';

export const documentApi = {
  uploadAsync: (file: File, sessionId: string) => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('sessionId', sessionId);
    // Don't set Content-Type manually: it would omit the multipart boundary and
    // break parsing server-side. Let axios/browser generate it automatically.
    return axiosInstance
      .post<UploadAsyncResponse>('/api/documents/upload-async', formData)
      .then((r) => r.data);
  },

  download: (fileId: string) =>
    axiosInstance
      .get(`/api/docs/${fileId}`, { responseType: 'blob' })
      .then((r) => r.data as Blob),
};
