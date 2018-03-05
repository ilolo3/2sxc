﻿using System;
using System.Collections.Generic;
using System.Linq;
using DotNetNuke.Entities.Modules;
using ToSic.Eav;
using ToSic.Eav.Apps;
using ToSic.Eav.Data.Query;
using ToSic.Eav.DataSources;
using ToSic.Eav.Logging;
using ToSic.Eav.Logging.Simple;
using ToSic.SexyContent.Interfaces;

namespace ToSic.SexyContent
{
	public class ContentGroupManager: HasLog
	{
		private const string ContentGroupTypeName = "2SexyContent-ContentGroup";

		private readonly int _zoneId;
		private readonly int _appId;
        private readonly bool _showDrafts;
        private readonly bool _enableVersioning;

        public ContentGroupManager(int zoneId, int appId, bool showDrafts, bool enableVersioning, Log parentLog): base("CG.Manage", parentLog)
		{
			_zoneId = zoneId;
			_appId = appId;
            _showDrafts = showDrafts;
            _enableVersioning = enableVersioning;
		}

		private IDataSource ContentGroupSource()
		{
			var dataSource = DataSource.GetInitialDataSource(_zoneId, _appId, _showDrafts);
			dataSource = DataSource.GetDataSource<EntityTypeFilter>(_zoneId, _appId, dataSource);
			((EntityTypeFilter)dataSource).TypeName = ContentGroupTypeName;
			return dataSource;
		}

		public IEnumerable<ContentGroup> GetContentGroups() 
            => ContentGroupSource().List.Select(p => new ContentGroup(p, _zoneId, _appId, _showDrafts, _enableVersioning, Log));

	    public ContentGroup GetContentGroup(Guid contentGroupGuid)
		{
		    Log.Add($"get CG#{contentGroupGuid}");
			var dataSource = ContentGroupSource();
			// ToDo: Should use an indexed guid source
		    var groupEntity = dataSource.List.One(contentGroupGuid);
		    return groupEntity != null 
                ? new ContentGroup(groupEntity, _zoneId, _appId, _showDrafts, _enableVersioning, Log) 
                : new ContentGroup(Guid.Empty, _zoneId, _appId, _showDrafts, _enableVersioning, Log) {DataIsMissing = true};
		}

		//public bool IsConfigurationInUse(int templateId, string type)
		//{
		//	var contentGroups = GetContentGroups().Where(p => p.Template != null && p.Template.TemplateId == templateId);
		//	return contentGroups.Any(p => p[type].Any(c => c != null));
		//}


	    /// <summary>
	    /// Saves a temporary templateId to the module's settings
	    /// This templateId will be used until a contentgroup exists
	    /// </summary>
	    public static void SetPreviewTemplate(int instanceId, Guid previewTemplateGuid) 
            => Factory.Resolve<IMapAppToInstance>().SetPreviewTemplate(instanceId, previewTemplateGuid);

	    public static void ClearPreviewTemplate(int instanceId) 
            => Factory.Resolve<IMapAppToInstance>().ClearPreviewTemplate(instanceId);

	    public Guid UpdateOrCreateContentGroup(ContentGroup contentGroup, int templateId)
		{
		    var appMan = new AppManager(_zoneId, _appId, Log);

		    if (!contentGroup.Exists)
		    {
		        Log.Add($"doesn't exist, will creat new CG with template#{templateId}");
		        return appMan.Entities.Create(ContentGroupTypeName, new Dictionary<string, object>
		        {
		            {"Template", new List<int> {templateId}},
		            {AppConstants.Content, new List<int>()},
		            {AppConstants.Presentation, new List<int>()},
		            {AppConstants.ListContent, new List<int>()},
		            {AppConstants.ListPresentation, new List<int>()}
		        }).Item2; // new guid
		    }
		    else
		    {
		        Log.Add($"exists, create for group#{contentGroup.ContentGroupGuid} with template#{templateId}");
		        appMan.Entities.UpdateParts(contentGroup._contentGroupEntity.EntityId,
		            new Dictionary<string, object> {{"Template", new List<int?> {templateId}}});

		        return contentGroup.ContentGroupGuid; // guid didn't change
		    }
		}

	    // todo: this doesn't look right, will have to mostly move to the new content-block
		public Tuple<Guid,Guid> GetInstanceContentGroup(int moduleId, int? pageId)
		{
		    var tabId = pageId ?? ModuleController.Instance.GetTabModulesByModule(moduleId)[0].TabID;

            Log.Add($"find content-group for mid#{moduleId} and tab#{tabId}");
			var settings = ModuleController.Instance.GetModule(moduleId, tabId, false).ModuleSettings;
			//var settings = moduleControl.GetModule(moduleId,).ModuleSettings;
		    var maybeGuid = settings[Settings.ContentGroupGuidString];
		    Guid.TryParse(maybeGuid?.ToString(), out var groupGuid);
            var previewTemplateString = settings[Settings.PreviewTemplateIdString]?.ToString();

		    var templateGuid = !string.IsNullOrEmpty(previewTemplateString)
		        ? Guid.Parse(previewTemplateString)
		        : new Guid();

		    return Tuple.Create(groupGuid, templateGuid);
		}

	    internal ContentGroup GetContentGroupOrGeneratePreview(Guid groupGuid, Guid previewTemplateGuid)
	    {
	        Log.Add($"get CG or gen preview for grp#{groupGuid}, preview#{previewTemplateGuid}");
	        // Return a "faked" ContentGroup if it does not exist yet (with the preview templateId)
	        return groupGuid == Guid.Empty 
                ? new ContentGroup(previewTemplateGuid, _zoneId, _appId, _showDrafts, _enableVersioning, Log)
                : GetContentGroup(groupGuid);
	    }

	}
}