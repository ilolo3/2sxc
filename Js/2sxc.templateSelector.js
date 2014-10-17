﻿(function () {
    var module = angular.module('2sxc.view', []);

    module.controller('TemplateSelectorCtrl', function($scope, $attrs, moduleApiService, groupApiService, $filter, $q) {

        var moduleId = $attrs.moduleid;

        var moduleApi = moduleApiService(moduleId);
        var groupApi = groupApiService(moduleId);

        $scope.manageInfo = $2sxc(moduleId).manage._manageInfo;
        $scope.apps = [];
        $scope.contentTypes = [];
        $scope.templates = [];
        $scope.filteredTemplates = function () {
            // Return all templates for App
            if (!$scope.manageInfo.isContentApp)
                return $scope.templates;
            return $filter('filter')($scope.templates, { AttributeSetID: $scope.contentTypeId });
        };
        $scope.contentTypeId = null;
        $scope.templateId = $scope.manageInfo.templateId;
        $scope.savedTemplateId = $scope.manageInfo.templateId;
        $scope.appId = $scope.manageInfo.appId;
        $scope.savedAppId = $scope.manageInfo.appId;

        $scope.reloadTemplates = function() {

            var getContentTypes = moduleApi.getSelectableContentTypes();
            var getTemplates = moduleApi.getSelectableTemplates();

            $q.all([getContentTypes, getTemplates]).then(function(res) {
                $scope.contentTypes = res[0].data;
                $scope.templates = res[1].data;
                var template = $filter('filter')($scope.templates, { TemplateID: $scope.templateId });
                if (template[0] != null && $scope.contentTypeId == null)
                    $scope.contentTypeId = template[0].AttributeSetID;

                $scope.$watch('templateId', function(newTemplateId, oldTemplateId) {
                    if (newTemplateId != oldTemplateId) {
                        $scope.renderTemplate(newTemplateId);
                    }
                });

                $scope.$watch('contentTypeId', function(newContentTypeId, oldContentTypeId) {
                    if (newContentTypeId == oldContentTypeId)
                        return;
                    // Select first template if contentType changed
                    var firstTemplateId = $filter('filter')($scope.templates, { AttributeSetID: $scope.contentTypeId })[0].TemplateID;
                    if ($scope.templateId != firstTemplateId && firstTemplateId != null)
                        $scope.templateId = firstTemplateId;
                });

            });

        };

        if ($scope.appId != null && $scope.manageInfo.templateChooserVisible)
            $scope.reloadTemplates();

        $scope.$watch('manageInfo.templateChooserVisible', function(visible) {
            if (visible)
                $scope.reloadTemplates();
        });

        $scope.$watch('appId', function (newAppId, oldAppId) {
            if (newAppId == oldAppId)
                return;
            moduleApi.setAppId(newAppId).then(function() {
                window.location.reload();
            });
        });

        if (!$scope.manageInfo.isContentApp) {
            moduleApi.getSelectableApps().then(function(data) {
                $scope.apps = data.data;
            });
        }

        $scope.setTemplateChooserState = function (state) {
            // Reset templateid / cancel template change
            if (!state)
                $scope.templateId = $scope.savedTemplateId;

            return moduleApi.setTemplateChooserState(state).then(function () {
                $scope.manageInfo.templateChooserVisible = state;
            });
        };

        $scope.saveTemplateId = function () {
            var promises = [];

            if ($scope.savedTemplateId != $scope.templateId) {
                promises.push(groupApi.saveTemplateId($scope.templateId));
            }

            $scope.savedTemplateId = $scope.templateId;
            promises.push($scope.setTemplateChooserState(false));

            return $q.all(promises);
        };

        $scope.renderTemplate = function (templateId) {
            moduleApi.renderTemplate(templateId).then(function (response) {
                $scope.insertRenderedTemplate(response.data);
                $2sxc(moduleId).manage._processToolbars();
            });
        };

        $scope.insertRenderedTemplate = function(renderedTemplate) {
            $(".DnnModule-" + moduleId + " .sc-viewport").html(renderedTemplate);
        };

        $scope.addItem = function(sortOrder) {
            groupApi.addItem(sortOrder).then(function () {
                $scope.renderTemplate($scope.templateId);
            });
        };

    });

    module.factory('moduleApiService', function(apiService) {
        return function(moduleId) {
            return {
                getSelectableApps: function() {
                    return apiService(moduleId, {
                        url: 'View/Module/GetSelectableApps'
                    });
                },
                setAppId: function(appId) {
                    return apiService(moduleId, {
                        url: 'View/Module/SetAppId',
                        params: { appId: appId }
                    });
                },
                getSelectableContentTypes: function () {
                    return apiService(moduleId, {
                        url: 'View/Module/GetSelectableContentTypes'
                    });
                },
                getSelectableTemplates: function() {
                    return apiService(moduleId, {
                        url: 'View/Module/GetSelectableTemplates'
                    });
                },
                setTemplateChooserState: function(state) {
                    return apiService(moduleId, {
                        url: 'View/Module/SetTemplateChooserState',
                        params: { state: state }
                    });
                },
                renderTemplate: function(templateId) {
                    return apiService(moduleId, {
                        url: 'View/Module/RenderTemplate',
                        params: { templateId: templateId }
                    });
                }
            };
        };
    });

    module.factory('groupApiService', function(apiService) {
        return function(moduleId) {
            return {
                saveTemplateId: function(templateId) {
                    return apiService(moduleId, {
                        url: 'View/ContentGroup/SaveTemplateId',
                        params: { templateId: templateId }
                    });
                },
                addItem: function(sortOrder) {
                    return apiService(moduleId, {
                        url: 'View/ContentGroup/AddItem',
                        params: { sortOrder: sortOrder }
                    });
                }
            };
        }
    });

    module.factory('apiService', function ($http) {

        return function (moduleId, settings) {

            var sf = $.ServicesFramework(moduleId);

            // Prepare HTTP headers for DNN Web API
            var headers = {
                ModuleId: sf.getModuleId(),
                TabId: sf.getTabId(),
                RequestVerificationToken: sf.getAntiForgeryValue()
            };

            settings.headers = $.extend({}, settings.headers, headers);
            settings.url = sf.getServiceRoot('2sxc') + settings.url;
            settings.params = $.extend({}, settings.params);
            return $http(settings);
        }
    });

})();


