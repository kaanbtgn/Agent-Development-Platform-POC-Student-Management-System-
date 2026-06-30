namespace StudentManagement.Agent.Services.Models;

/// <summary>
/// Agent yanıtı.
/// <para>
/// Not: <see cref="RequiresConfirmation"/> ve <see cref="ConfirmationPayload"/> şu an
/// yapısal olarak doldurulmamaktadır. Onay akışı System Prompt aracılığıyla LLM'e
/// bırakılmıştır — LLM, fuzzy match sonrası <c>requiresConfirmation=true</c> döndüğünde
/// kullanıcıdan doğal dil onayı ister.
/// </para>
/// </summary>
public sealed record AgentResponse(
    string Reply,
    bool RequiresConfirmation = false,
    ConfirmationPayload? ConfirmationPayload = null,
    OcrMetadata? OcrMetadata = null
);

public sealed record OcrMetadata(
    double OverallConfidence,
    bool RequiresHumanReview
);
