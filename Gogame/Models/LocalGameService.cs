using Avalonia.Threading;
using Gogame.Data;
using Gogame.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gogame.Models
{
    public class LocalGameService
    {
        public async Task SaveFinishedGameAsync(string sgf, string result, int? userId)
        {
            await using var db = new LocalDbContext();

            db.Games.Add(new GameRecordEntity
            {
                SgfContent = sgf,
                Result = result,
                UserId = userId
            });

            await db.SaveChangesAsync();
        }

        public class SyncService
        {
            private readonly ApiClient _api;
            private readonly SemaphoreSlim _lock = new(1, 1);

            public SyncService(ApiClient api) => _api = api;

            public async Task SyncAsync()
            {
                if (!App.Session.IsLoggedIn) return;
                if (!await _lock.WaitAsync(0)) return;

                try
                {
                    await using var db = new LocalDbContext();

                    var pending = await db.Games
                        .Where(g => !g.IsSynced && (g.UserId == null || g.UserId == App.Session.UserId))
                        .OrderBy(g => g.PlayedAt)
                        .Take(50)
                        .ToListAsync();

                    if (pending.Count == 0) return;

                    bool anyUploaded = false;

                    foreach(var game in pending)
                    {
                        var result = await _api.UploadGameAsync(new UploadGameDto
                        {
                            ClientId = game.ClientId,
                            Sgf = game.SgfContent,
                            Result = game.Result,
                            PlayedAtUtc = game.PlayedAt
                        });

                        if (!result.Success)
                        {
                            Debug.WriteLine("Sync stopped: " +
                        string.Join("; ", result.Errors.SelectMany(e => e.Value)));
                            break;
                        }

                        game.IsSynced = true;
                        game.ServerId = result.Data?.Id;
                        game.SyncedAt = DateTime.UtcNow;
                        game.UserId = App.Session.UserId;
                        await db.SaveChangesAsync();
                        anyUploaded = true;
                    }
                    if (anyUploaded)
                        await RefreshProfileAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sync failed: {ex.Message}");
                }
                finally
                {
                    _lock.Release();
                }
            }

            // Reload the stats
            private async Task RefreshProfileAsync()
            {
                var profile = await _api.GetProfileAsync();
                if(profile.Success && profile.Data != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => App.Session.SignIn(profile.Data, _api));
                }
            }
        }
    }
}
