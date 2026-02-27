using MAEMS.Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace MAEMS.Infrastructure.Services;

public class FirebaseAuthService : IFirebaseAuthService
{
    private readonly IConfiguration _configuration;
    private readonly FirebaseApp _firebaseApp;

    public FirebaseAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Initialize Firebase Admin SDK
        if (FirebaseApp.DefaultInstance == null)
        {
            var projectId = _configuration["Firebase:ProjectId"] 
                ?? throw new InvalidOperationException("Firebase ProjectId not configured");
            
            // Option 1: Use Application Default Credentials (recommended for production)
            // Make sure GOOGLE_APPLICATION_CREDENTIALS environment variable is set
            // pointing to your Firebase service account JSON file
            
            // Option 2: Use service account JSON file directly
            var credentialPath = _configuration["Firebase:CredentialPath"];
            
            if (!string.IsNullOrEmpty(credentialPath) && File.Exists(credentialPath))
            {
                _firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath),
                    ProjectId = projectId
                });
            }
            else
            {
                // Use default credentials
                _firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.GetApplicationDefault(),
                    ProjectId = projectId
                });
            }
        }
        else
        {
            _firebaseApp = FirebaseApp.DefaultInstance;
        }
    }

    public async Task<(bool IsValid, string? Email, string? Name)> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            // Verify the Firebase ID token using Firebase Admin SDK
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            
            if (decodedToken != null)
            {
                // Get user email from token claims
                string? email = decodedToken.Claims.ContainsKey("email") 
                    ? decodedToken.Claims["email"].ToString() 
                    : null;
                
                // Get user name from token claims
                string? name = decodedToken.Claims.ContainsKey("name") 
                    ? decodedToken.Claims["name"].ToString() 
                    : null;
                
                // If name is not available, try to get it from Firebase user record
                if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(decodedToken.Uid))
                {
                    try
                    {
                        var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(decodedToken.Uid);
                        name = userRecord.DisplayName;
                    }
                    catch
                    {
                        // If getting user record fails, continue with name as null
                    }
                }

                return (true, email, name);
            }

            return (false, null, null);
        }
        catch (FirebaseAuthException ex)
        {
            // Log the Firebase authentication error for debugging
            Console.WriteLine($"Firebase Auth Error: {ex.Message}");
            return (false, null, null);
        }
        catch (Exception ex)
        {
            // Log general errors
            Console.WriteLine($"Error validating token: {ex.Message}");
            return (false, null, null);
        }
    }
}
