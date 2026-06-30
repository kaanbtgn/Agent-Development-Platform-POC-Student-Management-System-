export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: string;
  isStreaming?: boolean;
}

export interface OcrProgress {
  step: string;
  progressPercent: number;
}

export interface ChatRequest {
  message: string;
  sessionId: string;
}

export interface UploadAsyncResponse {
  jobId: string;
  sessionId: string;
}

/** Agent'ın döndürdüğü yanıt. API /api/chat endpoint'inden gelir. */
export interface AgentResponse {
  reply: string;
  requiresConfirmation: boolean;
  confirmationPayload: ConfirmationPayload | null;
  ocrMetadata: OcrMetadata | null;
}

export interface OcrMetadata {
  overallConfidence: number;
  requiresHumanReview: boolean;
}

export interface ConfirmationPayload {
  matchedStudentId: string;
  originalName: string;
  matchedName: string;
  score: number;
  pendingAction: string;
  pendingActionArgs: string;
}
