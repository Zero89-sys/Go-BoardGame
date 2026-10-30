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
        // Game record save
        public async Task SaveFinishedGameAsync(string sgf, string result, int? userId, string playerColor, bool vsBot)
        {
            await using var db = new LocalDbContext();

            db.Games.Add(new GameRecordEntity
            {
                SgfContent = sgf,
                Result = result,
                UserId = userId,
                PlayerColor = playerColor,
                VsBot = vsBot
            });

            await db.SaveChangesAsync();
        }

        public class SyncService
        {
            private readonly ApiClient _api;
            private readonly SemaphoreSlim _lock = new(1, 1);

            public SyncService(ApiClient api) => _api = api;

            // Sending saved game records to the server
            public async Task SyncAsync()
            {
                if (!App.Session.IsLoggedIn || App.Session.UserId is not int userId)
                {
                    Debug.WriteLine($"[SYNC] skipped: loggedIn={App.Session.IsLoggedIn}, userId={App.Session.UserId}");
                    return;
                }
                if (!await _lock.WaitAsync(0))
                {
                    Debug.WriteLine("[SYNC] skipped: already running");
                    return;
                }

                try
                {
                    await using var db = new LocalDbContext();

                    // Up to 50 locally stored games to sync
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
                            PlayedAtUtc = DateTime.SpecifyKind(game.PlayedAt, DateTimeKind.Utc),
                            PlayerColor = game.PlayerColor ?? "B",
                            VsBot = game.VsBot,
                        });

                        if (!result.Success)
                        {
                            Debug.WriteLine("Sync stopped: " +
                            string.Join("; ", result.Errors.SelectMany(e => e.Value)));
                            if (result.StatusCode == 400) continue;
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
