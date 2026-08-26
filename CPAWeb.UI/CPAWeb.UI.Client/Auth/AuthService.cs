using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CPAWeb.Services.DTOs;

namespace CPAWeb.UI.Client.Auth
{
    // Մեկ տեղում հավաքված են auth/users API-ի կանչերը
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly JwtAuthenticationStateProvider _authStateProvider;

        public AuthService(HttpClient http, JwtAuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _authStateProvider = authStateProvider;
        }

        public async Task<LoginResultDto> LoginAsync(LoginRequestDto request)
        {
            HttpResponseMessage response;

            try
            {
                response = await _http.PostAsJsonAsync("api/auth/login", request);
            }
            catch (HttpRequestException)
            {
                return new LoginResultDto { Success = false, Message = "cannot reach the server." };
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var failure = await ReadOrDefault<LoginResultDto>(response);
                return failure ?? new LoginResultDto { Success = false, Message = "invalid username or password." };
            }

            if (!response.IsSuccessStatusCode)
                return new LoginResultDto { Success = false, Message = "login failed, please try again." };

            var result = await ReadOrDefault<LoginResultDto>(response);

            if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.Token))
                return new LoginResultDto { Success = false, Message = "invalid response from the server." };

            await _authStateProvider.SignInAsync(result.Token);
            return result;
        }

        public Task LogoutAsync() => _authStateProvider.SignOutAsync();

        // "users" էջի ցանկը (միայն ադմին)
        public async Task<List<UserDto>> GetUsersAsync()
        {
            var response = await _http.GetAsync("api/users");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new List<UserDto>();
        }

        // Նոր օգտատեր՝ անուն, email, գաղտնաբառ (միայն ադմին)
        public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto)
        {
            HttpResponseMessage response;

            try
            {
                response = await _http.PostAsJsonAsync("api/users", dto);
            }
            catch (HttpRequestException)
            {
                return new CreateUserResultDto { Success = false, Message = "cannot reach the server." };
            }

            var result = await ReadOrDefault<CreateUserResultDto>(response);

            return result ?? new CreateUserResultDto
            {
                Success = false,
                Message = response.IsSuccessStatusCode ? "invalid response from the server." : "the user was not added."
            };
        }

        // Ջնջում (միայն ադմին)
        public async Task<DeleteUserResultDto> DeleteUserAsync(long id)
        {
            HttpResponseMessage response;

            try
            {
                response = await _http.DeleteAsync($"api/users/{id}");
            }
            catch (HttpRequestException)
            {
                return new DeleteUserResultDto { Success = false, Message = "cannot reach the server." };
            }

            var result = await ReadOrDefault<DeleteUserResultDto>(response);

            return result ?? new DeleteUserResultDto
            {
                Success = false,
                Message = response.IsSuccessStatusCode ? "invalid response from the server." : "the user was not deleted."
            };
        }

        private static async Task<T?> ReadOrDefault<T>(HttpResponseMessage response) where T : class
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException || ex is NotSupportedException)
            {
                return null;
            }
        }
    }
}
