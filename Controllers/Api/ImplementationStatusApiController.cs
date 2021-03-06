using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GoNorth.Data.Aika;
using GoNorth.Data.Karta;
using GoNorth.Data.Karta.Marker;
using GoNorth.Data.Kortisto;
using GoNorth.Data.Styr;
using GoNorth.Data.Tale;
using GoNorth.Services.ImplementationStatusCompare;
using GoNorth.Services.Timeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace GoNorth.Controllers.Api
{
    /// <summary>
    /// Implementation Status controller
    /// </summary>
    [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
    [Route("/api/[controller]/[action]")]
    public class ImplementationStatusApiController : Controller
    {
        /// <summary>
        /// Formatted Compare Response
        /// </summary>
        public class FormattedCompareResponse
        {
            /// <summary>
            /// true if the object was implemented before, else false
            /// </summary>
            public bool DoesSnapshotExist { get; set; }

            /// <summary>
            /// Differences if the object was implemented before
            /// </summary>
            /// <returns></returns>
            public List<CompareDifferenceFormatted> CompareDifference { get; set; }
        };

        /// <summary>
        /// Implementation status comparer
        /// </summary>
        private readonly IImplementationStatusComparer _implementationStatusComparer;

        /// <summary>
        /// Npc Db Access
        /// </summary>
        private readonly IKortistoNpcDbAccess _npcDbAccess;
        
        /// <summary>
        /// Npc Implementation Snapshot Db Access
        /// </summary>
        private readonly IKortistoNpcImplementationSnapshotDbAccess _npcSnapshotDbAccess;
        
        /// <summary>
        /// Item Db Access
        /// </summary>
        private readonly IStyrItemDbAccess _itemDbAccess;

        /// <summary>
        /// Item Implementation Snapshot Db Access
        /// </summary>
        private readonly IStyrItemImplementationSnapshotDbAccess _itemSnapshotDbAccess;

        /// <summary>
        /// Dialog Db Access
        /// </summary>
        private readonly ITaleDbAccess _dialogDbAccess;

        /// <summary>
        /// Dialog  Implementation Snapshot Db Access
        /// </summary>
        private readonly ITaleDialogImplementationSnapshotDbAccess _dialogSnapshotDbAccess;

        /// <summary>
        /// Quest Db Access
        /// </summary>
        private readonly IAikaQuestDbAccess _questDbAccess;

        /// <summary>
        /// Quest Implementation Snapshot Db Access
        /// </summary>
        private readonly IAikaQuestImplementationSnapshotDbAccess _questSnapshotDbAccess;

        /// <summary>
        /// Karta Map Db Access
        /// </summary>
        private readonly IKartaMapDbAccess _mapDbAccess;

        /// <summary>
        /// Karta Marker Implementation Snapshot Db Access
        /// </summary>
        private readonly IKartaMarkerImplementationSnapshotDbAccess _markerSnapshotDbAccess;

        /// <summary>
        /// Timeline Service
        /// </summary>
        private readonly ITimelineService _timelineService;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Localizer
        /// </summary>
        private readonly IStringLocalizer _localizer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="implementationStatusComparer">Implementation status comparer</param>
        /// <param name="npcDbAccess">Npc Db Access</param>
        /// <param name="npcSnapshotDbAccess">Npc Implementation Snapshot Db Access</param>
        /// <param name="itemDbAccess">Item Db Access</param>
        /// <param name="itemSnapshotDbAccess">Item Implementation Snapshot Db Access</param>
        /// <param name="dialogDbAccess">Dialog Db Access</param>
        /// <param name="dialogSnapshotDbAccess">Dialog Implementation Snapshot Db Access</param>
        /// <param name="questDbAccess">Quest Db Access</param>
        /// <param name="questSnapshotDbAccess">Quest Implementation Snapshot Db Access</param>
        /// <param name="mapDbAccess">Map Db Access</param>
        /// <param name="markerSnapshotDbAccess">Marker Db Access</param>
        /// <param name="timelineService">Timeline Service</param>
        /// <param name="logger">Logger</param>
        /// <param name="localizerFactory">Localizer Factory</param>
        public ImplementationStatusApiController(IImplementationStatusComparer implementationStatusComparer, IKortistoNpcDbAccess npcDbAccess, IKortistoNpcImplementationSnapshotDbAccess npcSnapshotDbAccess, IStyrItemDbAccess itemDbAccess, IStyrItemImplementationSnapshotDbAccess itemSnapshotDbAccess,
                                                 ITaleDbAccess dialogDbAccess, ITaleDialogImplementationSnapshotDbAccess dialogSnapshotDbAccess, IAikaQuestDbAccess questDbAccess, IAikaQuestImplementationSnapshotDbAccess questSnapshotDbAccess, IKartaMapDbAccess mapDbAccess, IKartaMarkerImplementationSnapshotDbAccess markerSnapshotDbAccess, 
                                                 ITimelineService timelineService, ILogger<ImplementationStatusApiController> logger, IStringLocalizerFactory localizerFactory)
        {
            _implementationStatusComparer = implementationStatusComparer;
            _npcDbAccess = npcDbAccess;
            _npcSnapshotDbAccess = npcSnapshotDbAccess;
            _itemDbAccess = itemDbAccess;
            _itemSnapshotDbAccess = itemSnapshotDbAccess;
            _dialogDbAccess = dialogDbAccess;
            _dialogSnapshotDbAccess = dialogSnapshotDbAccess;
            _questDbAccess = questDbAccess;
            _questSnapshotDbAccess = questSnapshotDbAccess;
            _mapDbAccess = mapDbAccess;
            _markerSnapshotDbAccess = markerSnapshotDbAccess;
            _timelineService = timelineService;
            _logger = logger;
            _localizer = localizerFactory.Create(typeof(ImplementationStatusApiController));
        }

        /// <summary>
        /// Returns the compare results for an npc
        /// </summary>
        /// <param name="npcId">Id of the npc</param>
        /// <returns>Compare results</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Kortisto)]
        [HttpGet]
        public async Task<IActionResult> CompareNpc(string npcId)
        {
            CompareResult result = await _implementationStatusComparer.CompareNpc(npcId);
            
            FormattedCompareResponse response = await BuildFormattedResponse(result);
            return Ok(response);
        }

        /// <summary>
        /// Flags an npc as implemented
        /// </summary>
        /// <param name="npcId">Id of the npc</param>
        /// <returns>Result</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Kortisto)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> FlagNpcAsImplemented(string npcId)
        {
            // Check Data
            KortistoNpc npc = await _npcDbAccess.GetFlexFieldObjectById(npcId);
            if(npc == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound);
            }

            // Flag npc as implemented
            npc.IsImplemented = true;
            await _npcSnapshotDbAccess.SaveSnapshot(npc);
            await _npcDbAccess.UpdateFlexFieldObject(npc);

            // Add Timeline entry
            await _timelineService.AddTimelineEntry(TimelineEvent.ImplementedNpc, npc.Id, npc.Name);

            return Ok();
        }


        /// <summary>
        /// Returns the compare results for an item
        /// </summary>
        /// <param name="itemId">Id of the item</param>
        /// <returns>Compare results</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Styr)]
        [HttpGet]
        public async Task<IActionResult> CompareItem(string itemId)
        {
            CompareResult result = await _implementationStatusComparer.CompareItem(itemId);
            
            FormattedCompareResponse response = await BuildFormattedResponse(result);
            return Ok(response);
        }

        /// <summary>
        /// Flags an item as implemented
        /// </summary>
        /// <param name="itemId">Id of the item</param>
        /// <returns>Result</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Styr)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> FlagItemAsImplemented(string itemId)
        {
            // Check Data
            StyrItem item = await _itemDbAccess.GetFlexFieldObjectById(itemId);
            if(item == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound);
            }

            // Flag item as implemented
            item.IsImplemented = true;
            await _itemSnapshotDbAccess.SaveSnapshot(item);
            await _itemDbAccess.UpdateFlexFieldObject(item);

            // Add Timeline entry
            await _timelineService.AddTimelineEntry(TimelineEvent.ImplementedItem, item.Id, item.Name);

            return Ok();
        }


        /// <summary>
        /// Returns the compare results for a dialog
        /// </summary>
        /// <param name="dialogId">Id of the dialog</param>
        /// <returns>Compare results</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Tale)]
        [HttpGet]
        public async Task<IActionResult> CompareDialog(string dialogId)
        {
            CompareResult result = await _implementationStatusComparer.CompareDialog(dialogId);
            
            FormattedCompareResponse response = await BuildFormattedResponse(result);
            return Ok(response);
        }

        /// <summary>
        /// Flags a dialog as implemented
        /// </summary>
        /// <param name="dialogId">Id of the dialog</param>
        /// <returns>Result</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Tale)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> FlagDialogAsImplemented(string dialogId)
        {
            // Check Data
            TaleDialog dialog = await _dialogDbAccess.GetDialogById(dialogId);
            if(dialog == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound);
            }

            // Flag dialog as implemented
            dialog.IsImplemented = true;
            await _dialogSnapshotDbAccess.SaveSnapshot(dialog);
            await _dialogDbAccess.UpdateDialog(dialog);

            // Add Timeline entry
            List<KortistoNpc> npcNames = await _npcDbAccess.ResolveFlexFieldObjectNames(new List<string> { dialog.RelatedObjectId });
            string npcName = "";
            if(npcNames.Count > 0)
            {
                npcName = npcNames[0].Name;
            }
            await _timelineService.AddTimelineEntry(TimelineEvent.ImplementedDialog, dialog.RelatedObjectId, npcName);

            return Ok();
        }


        /// <summary>
        /// Returns the compare results for a quest
        /// </summary>
        /// <param name="questId">Id of the quest</param>
        /// <returns>Compare results</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Aika)]
        [HttpGet]
        public async Task<IActionResult> CompareQuest(string questId)
        {
            CompareResult result = await _implementationStatusComparer.CompareQuest(questId);
            
            FormattedCompareResponse response = await BuildFormattedResponse(result);
            return Ok(response);
        }

        /// <summary>
        /// Flags a quest as implemented
        /// </summary>
        /// <param name="questId">Id of the Quest</param>
        /// <returns>Result</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Aika)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> FlagQuestAsImplemented(string questId)
        {
            // Check Data
            AikaQuest quest = await _questDbAccess.GetQuestById(questId);
            if(quest == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound);
            }

            // Flag quest as implemented
            quest.IsImplemented = true;
            await _questSnapshotDbAccess.SaveSnapshot(quest);
            await _questDbAccess.UpdateQuest(quest);

            // Add Timeline entry
            await _timelineService.AddTimelineEntry(TimelineEvent.ImplementedQuest, quest.Id, quest.Name);

            return Ok();
        }


        /// <summary>
        /// Returns the compare results for a marker
        /// </summary>
        /// <param name="mapId">Id of the map</param>
        /// <param name="markerId">Id of the marker</param>
        /// <param name="markerType">Type of the marker</param>
        /// <returns>Compare results</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Karta)]
        [HttpGet]
        public async Task<IActionResult> CompareMarker(string mapId, string markerId, MarkerType markerType)
        {
            CompareResult result = await _implementationStatusComparer.CompareMarker(mapId, markerId, markerType);
            
            FormattedCompareResponse response = await BuildFormattedResponse(result);
            return Ok(response);
        }

        /// <summary>
        /// Flags a quest as implemented
        /// </summary>
        /// <param name="mapId">Id of the map</param>
        /// <param name="markerId">Id of the marker</param>
        /// <param name="markerType">Type of the marker</param>
        /// <returns>Result</returns>
        [Authorize(Roles = RoleNames.ImplementationStatusTracker)]
        [Authorize(Roles = RoleNames.Karta)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> FlagMarkerAsImplemented(string mapId, string markerId, MarkerType markerType)
        {
            // Check Data
            KartaMap map = await _mapDbAccess.GetMapById(mapId);
            if(map == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound);
            }

            // Flag Marker as implemented
            if(markerType == MarkerType.Npc && map.NpcMarker != null)
            {
                NpcMapMarker marker = map.NpcMarker.First(m => m.Id == markerId);
                marker.IsImplemented = true;
                await _markerSnapshotDbAccess.SaveNpcMarkerSnapshot(marker);
                await _mapDbAccess.UpdateMap(map);
            }
            else if(markerType == MarkerType.Item && map.ItemMarker != null)
            {
                ItemMapMarker marker = map.ItemMarker.First(m => m.Id == markerId);
                marker.IsImplemented = true;
                await _markerSnapshotDbAccess.SaveItemMarkerSnapshot(marker);
                await _mapDbAccess.UpdateMap(map);
            }
            else if(markerType == MarkerType.MapChange && map.MapChangeMarker != null)
            {
                MapChangeMapMarker marker = map.MapChangeMarker.First(m => m.Id == markerId);
                marker.IsImplemented = true;
                await _markerSnapshotDbAccess.SaveMapChangeMarkerSnapshot(marker);
                await _mapDbAccess.UpdateMap(map);
            }
            else if(markerType == MarkerType.Quest && map.MapChangeMarker != null)
            {
                QuestMapMarker marker = map.QuestMarker.First(m => m.Id == markerId);
                marker.IsImplemented = true;
                await _markerSnapshotDbAccess.SaveQuestMarkerSnapshot(marker);
                await _mapDbAccess.UpdateMap(map);
            }

            // Add Timeline entry
            await _timelineService.AddTimelineEntry(TimelineEvent.ImplementedMarker, mapId, markerId, markerType.ToString(), map.Name);

            return Ok();
        }

                
        /// <summary>
        /// Builds a formatted response from a compare result
        /// </summary>
        /// <param name="result">Compare result</param>
        /// <returns>Formatted response</returns>
        private async Task<FormattedCompareResponse> BuildFormattedResponse(CompareResult result)
        {
            FormattedCompareResponse response = new FormattedCompareResponse();
            response.DoesSnapshotExist = result.DoesSnapshotExist;
            if(result.DoesSnapshotExist)
            {
                response.CompareDifference = await _implementationStatusComparer.FormatCompareResult(result.CompareDifference);
            }

            return response;
        }

    }
}