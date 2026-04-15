using MAEMS.Application.Interfaces;
using MAEMS.Application.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MAEMS.Infrastructure.Services;

public class OtpService : IOtpService
{
    private static readonly ConcurrentDictionary<string, OtpEntry> _otpStore = new();

    public string GenerateOtp()
    {
        // Generate cryptographically secure 6-digit OTP
        var randomNumber = RandomNumberGenerator.GetInt32(100000, 1000000);
        return randomNumber.ToString();
    }

    public Task StoreOtpAsync(string email, string otpCode, int userId, TimeSpan expiration)
    {
        var entry = new OtpEntry
        {
            Email = email.ToLowerInvariant(),
            OtpCode = otpCode,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.Add(expiration)
        };

        _otpStore.AddOrUpdate(email.ToLowerInvariant(), entry, (key, oldValue) => entry);

        // Clean up expired OTPs
        CleanupExpiredOtps();

        return Task.CompletedTask;
    }

    public Task<(bool IsValid, int UserId)> ValidateOtpAsync(string email, string otpCode)
    {
        var normalizedEmail = email.ToLowerInvariant();

        if (!_otpStore.TryGetValue(normalizedEmail, out var entry))
        {
            return Task.FromResult((false, 0));
        }

        // Check if OTP is expired
        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            _otpStore.TryRemove(normalizedEmail, out _);
            return Task.FromResult((false, 0));
        }

        // Check if OTP matches
        if (entry.OtpCode != otpCode)
        {
            return Task.FromResult((false, 0));
        }

        return Task.FromResult((true, entry.UserId));
    }

    public Task RevokeOtpAsync(string email)
    {
        _otpStore.TryRemove(email.ToLowerInvariant(), out _);
        return Task.CompletedTask;
    }

    private void CleanupExpiredOtps()
    {
        var expiredKeys = _otpStore
            .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _otpStore.TryRemove(key, out _);
        }
    }
}
