﻿namespace SurveyBasket.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> GetTokenAsync(string email, string password, CancellationToken cancellationToken = default);
}