using System.Collections.Generic;
using EmmaServer.Entities;
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;


namespace EmmaClientAv.Services;

public interface ILoginService
{
    Task<LoginResponse> LoginAsync(string user, string password);
}

public class LoginService : ILoginService
{
    private readonly HttpClient Client;

    public LoginService()
    {
        Client = new HttpClient();
    }

    public async Task<LoginResponse> LoginAsync(string user, string password)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/v1/auth";
        
        using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);

        // Codifica "username:password" in Base64
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));

        // Aggiungi l'header Authorization nel formato "Basic [Token]"
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        HttpResponseMessage response = await Client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return  await response.Content.ReadFromJsonAsync<LoginResponse>();
            
        }
        else
        {
            throw new Exception($"Errore durante l'invio: {response.StatusCode} {response.Content}");
        }
    }
}