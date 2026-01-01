using System.ComponentModel;

public sealed class BotInfoService
{
    private readonly DatabaseHelper _dbh;
    private readonly DiscordLookupService _lookup;
    private readonly MotdService _motdService;
    public BotInfoService(DatabaseHelper aDb, 
                        DiscordLookupService aLookup, 
                        MotdService aMotdService)
    {
        _dbh = aDb;
        _lookup = aLookup;
        _motdService = aMotdService;
    }
    public async Task<BotInfoDTO> GetChannelInfo()
    {
        string lMotdChannelID = _dbh.GetMotdChannelID() ?? string.Empty;
        string lWeightedChannelID = _dbh.GetWeightedChannelID() ?? string.Empty;
        string lTargetUserID = _dbh.GetTargetUserID() ?? string.Empty;
        string lTargetChannelID = _dbh.GetTargetChannelID() ?? string.Empty;
        
        return new BotInfoDTO()
        {
            MotdChannel = await ResolveChannelName(lMotdChannelID),
            WeightedChannel = await ResolveChannelName(lWeightedChannelID),
            TargetUser = await ResolveUserName(lTargetUserID),
            TargetChannel = await ResolveChannelName(lTargetChannelID),
            HasMotdBeenPosted = await _motdService.HasMotdBeenPostedAsync(DateTime.UtcNow.Date)
        };
    }
    private async Task<string?> ResolveChannelName(string? aID)
    {
        if (string.IsNullOrWhiteSpace(aID)) 
            return null;
        if(!ulong.TryParse(aID, out var lChannelID)) 
            return null;
        return await _lookup.GetDiscordChannelAsync(lChannelID);
    }
    private async Task<string?> ResolveUserName(string? aID)
    {
        if (string.IsNullOrWhiteSpace(aID)) 
            return null;
        if(!ulong.TryParse(aID, out var lUserID)) 
            return null;
        return await _lookup.GetDiscordUserAsync(lUserID);
    }
}