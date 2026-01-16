using DSharpPlus.Entities;

public record WebhookResult(bool IsSuccess, DiscordWebhook? Webhook, string? Error);