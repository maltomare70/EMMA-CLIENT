using System.Collections.Generic;
using EmmaServer.Entities;
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Linq;

namespace EmmaClientAv.Services;

public interface IArticoliService
{
    Task<List<EmmaArticoli>> GetArticoliFornitore(string descrizione);
    Task AddArticolo(EmmaArticoli articolo);
    Task UpdateArticolo(EmmaArticoli articolo);
    Task DeleteArticolo(EmmaArticoli articolo);
}
public class ArticoliService : IArticoliService
{
    private readonly HttpClient Client;

    public ArticoliService()
    {
        Client = new HttpClient();
    }
    
    public async  Task<List<EmmaArticoli>> GetArticoliFornitore(string descrizione)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli?fornitore={descrizione}";
        using var request = new HttpRequestMessage(HttpMethod.Get, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var emmaArticoliList = await response.Content.ReadFromJsonAsync<List<EmmaArticoli>>().ConfigureAwait(false);
            if (emmaArticoliList != null)
            {
                emmaArticoliList =  emmaArticoliList.OrderBy(x => x.descrizione).ToList();
            }

            return emmaArticoliList;
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new ApplicationException(errorContent); 
        }
    }
    
    public async Task AddArticolo(EmmaArticoli articolo)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli";
        using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(articolo);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            //
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new ApplicationException(errorContent); 
        }
    }
    
    public async Task UpdateArticolo(EmmaArticoli articolo)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli";
        using var request = new HttpRequestMessage(HttpMethod.Put, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(articolo);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            //
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new ApplicationException(errorContent); 
        }
    }
    
    public async Task DeleteArticolo(EmmaArticoli articolo)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli";
        using var request = new HttpRequestMessage(HttpMethod.Delete, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(articolo);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            //
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new ApplicationException(errorContent); 
        }
    }
}