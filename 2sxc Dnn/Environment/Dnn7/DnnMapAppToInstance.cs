﻿using System;
using System.IO;
using System.Linq;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using ToSic.Eav;
using ToSic.Eav.Apps;
using ToSic.Eav.Apps.Environment;
using ToSic.Eav.Apps.Interfaces;
using ToSic.Eav.DataSources.Caches;
using ToSic.Eav.Logging.Simple;
using ToSic.SexyContent.Interfaces;
using ToSic.SexyContent.Internal;

namespace ToSic.SexyContent.Environment.Dnn7
{

    public class DnnMapAppToInstance : IMapAppToInstance
    {

        public int? GetAppIdFromInstance(IInstanceInfo instance, int zoneId)
        {
            var module = (instance as EnvironmentInstance<ModuleInfo>).Original;

            if (module.DesktopModule.ModuleName == "2sxc")
                return new ZoneRuntime(zoneId, null).DefaultAppId;

            if(module.ModuleSettings.ContainsKey(Settings.AppNameString))
                return AppHelpers.GetAppIdFromGuidName(zoneId, module.ModuleSettings[Settings.AppNameString].ToString());
            
            return null;
        }


        
        public void SetAppIdForInstance(IInstanceInfo instance, IEnvironment env, int? appId, Log parentLog)
        {
            // Reset temporary template
            ContentGroupManager.ClearPreviewTemplate(instance.Id);

            // ToDo: Should throw exception if a real ContentGroup exists

            var module = (instance as EnvironmentInstance<ModuleInfo>).Original;
            var zoneId = env.ZoneMapper.GetZoneId(module.OwnerPortalID);

            if (appId == 0 || !appId.HasValue)
                DnnStuffToRefactor.UpdateInstanceSettingForAllLanguages(instance.Id, Settings.AppNameString, null);
            else
            {
                var appName = ((BaseCache)DataSource.GetCache(0, 0)).ZoneApps[zoneId].Apps[appId.Value];
                DnnStuffToRefactor.UpdateInstanceSettingForAllLanguages(instance.Id, Settings.AppNameString, appName);
            }

            // Change to 1. available template if app has been set
            if (appId.HasValue)
            {
                var app = new App(new DnnTenant(null), zoneId, appId.Value);
                var templateGuid = app.TemplateManager.GetAllTemplates().FirstOrDefault(t => !t.IsHidden)?.Guid;
                if (templateGuid.HasValue)
                    ContentGroupManager.SetPreviewTemplate(instance.Id, templateGuid.Value);
            }
        }


        public void ClearPreviewTemplate(int instanceId)
        {
            DnnStuffToRefactor.UpdateInstanceSettingForAllLanguages(instanceId, Settings.PreviewTemplateIdString, null);
        }

        public void SetContentGroupAndBlankTemplate(int instanceId, bool wasCreated, Guid guid)
        {
            // Remove the previewTemplateId (because it's not needed as soon Content is inserted)
            ClearPreviewTemplate(instanceId);
            // Update contentGroup Guid for this module
            if (wasCreated)
                DnnStuffToRefactor.UpdateInstanceSettingForAllLanguages(instanceId, Settings.ContentGroupGuidString,
                    guid.ToString());
        }

        public ContentGroup GetInstanceContentGroup(ContentGroupManager cgm, Log log, int instanceId, int? pageId)
        {
            var mci = ModuleController.Instance;

            var tabId = pageId ?? mci.GetTabModulesByModule(instanceId)[0].TabID;

            log.Add($"find content-group for mid#{instanceId} and tab#{tabId}");
            var settings = mci.GetModule(instanceId, tabId, false).ModuleSettings;

            var maybeGuid = settings[Settings.ContentGroupGuidString];
            Guid.TryParse(maybeGuid?.ToString(), out var groupGuid);
            var previewTemplateString = settings[Settings.PreviewTemplateIdString]?.ToString();

            var templateGuid = !string.IsNullOrEmpty(previewTemplateString)
                ? Guid.Parse(previewTemplateString)
                : new Guid();

            return cgm.GetContentGroupOrGeneratePreview(groupGuid, templateGuid);
        }

        /// <summary>
        /// Saves a temporary templateId to the module's settings
        /// This templateId will be used until a contentgroup exists
        /// </summary>
        public void SetPreviewTemplate(int instanceId, Guid previewTemplateGuid)
        {
            // todo: 2rm - I believe you are accidentally using uncached module settings access - pls check and probably change
            // todo: note: this is done ca. 3x in this class
            var moduleController = new ModuleController();
            var settings = moduleController.GetModule(instanceId).ModuleSettings;// older, deprecated api: .GetModuleSettings(instanceId);

            // Do not allow saving the temporary template id if a contentgroup exists for this module
            if (settings[Settings.ContentGroupGuidString] != null)
                throw new Exception("Preview template id cannot be set for a module that already has content.");

            DnnStuffToRefactor.UpdateInstanceSettingForAllLanguages(instanceId, Settings.PreviewTemplateIdString, previewTemplateGuid.ToString());
        }

        public void UpdateTitle(SxcInstance sxcInstance, Eav.Interfaces.IEntity titleItem)
        {
            //Log.Add("update title");

            var languages = sxcInstance.Environment.ZoneMapper.CulturesWithState(sxcInstance.EnvInstance.TenantId,
                sxcInstance.ZoneId.Value);

            // Find Module for default language
            var moduleController = new ModuleController();
            var originalModule = moduleController.GetModule(sxcInstance.EnvInstance.Id);

            foreach (var dimension in languages)
            {
                if (!originalModule.IsDefaultLanguage)
                    originalModule = originalModule.DefaultLanguageModule;

                try // this can sometimes fail, like if the first item is null - https://github.com/2sic/2sxc/issues/817
                {
                    // Break if default language module is null
                    if (originalModule == null)
                        return;

                    // Get Title value of Entitiy in current language
                    var titleValue = titleItem.Title[dimension.Key].ToString();

                    // Find module for given Culture
                    var moduleByCulture = moduleController.GetModuleByCulture(originalModule.ModuleID,
                        originalModule.TabID, sxcInstance.EnvInstance.TenantId,
                        DotNetNuke.Services.Localization.LocaleController.Instance.GetLocale(dimension.Key));

                    // Break if no title module found
                    if (moduleByCulture == null || titleValue == null)
                        return;

                    moduleByCulture.ModuleTitle = titleValue;
                    moduleController.UpdateModule(moduleByCulture);
                }
                catch
                {
                    // ignored
                }
            }
        }

        // todo: remove this, replace with calls to the current tenant -> RootPath
        public static string AppBasePath() 
            => Path.Combine(PortalSettings.Current.HomeDirectory, Settings.AppsRootFolder);
    }
}