import axiosInstance from './axiosInstance';
import type { AgentResponse } from '@/types/agent.types';

export const agentApi = {
  // sessionId is injected automatically via X-Session-Id header in axiosInstance
  chat: (message: string) =>
    axiosInstance.post<AgentResponse>('/api/chat', { message }).then((r) => r.data),

  chatWithDocument: (message: string, file: File) => {
    const formData = new FormData();
    formData.append('message', message);
    formData.append('file', file);
    // sessionId is sent via X-Session-Id header by axiosInstance interceptor
    return axiosInstance
      .post<AgentResponse>('/api/chat/document', formData)
      .then((r) => r.data);
  },
};
