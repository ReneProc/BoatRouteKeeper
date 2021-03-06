#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2013
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Utilities;

using Globals = DotNetNuke.Common.Globals;


#endregion

namespace DotNetNuke.Modules.Admin.Languages
{
    /// -----------------------------------------------------------------------------
    /// <summary>
    ///   The EditLanguage ModuleUserControlBase is used to edit a Language
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <history>
    ///   [cnurse]	02/14/2008  created
    /// </history>
    /// -----------------------------------------------------------------------------
    public partial class EditLanguage : PortalModuleBase
    {
        private Locale _Language;

        #region "Protected Properties"

        protected bool IsAddMode
        {
            get
            {
                return string.IsNullOrEmpty(Request.QueryString["locale"]);
            }
        }

        protected Locale Language
        {
            get
            {
                if (!IsAddMode)
                {
                    _Language = LocaleController.Instance.GetLocale(int.Parse(Request.QueryString["locale"]));
                }
                return _Language;
            }
        }

        #endregion

        #region "Private Methods"

        /// -----------------------------------------------------------------------------
        /// <summary>
        ///   This routine Binds the Language
        /// </summary>
        /// <history>
        ///   [cnurse]	02/14/2008  created
        /// </history>
        /// -----------------------------------------------------------------------------
        private void BindLanguage()
        {
            languageLanguageLabel.Visible = (Language != null);
            languageComboBox.Visible = Language == null;
            languageComboBox.IncludeNoneSpecified = false;
            languageComboBox.HideLanguagesList = LocaleController.Instance.GetLocales(Null.NullInteger);
            languageComboBox.DataBind();

            fallbackLanguageLabel.Visible = !UserInfo.IsSuperUser;
            fallBackComboBox.Visible = UserInfo.IsSuperUser;
            fallBackComboBox.IncludeNoneSpecified = true;
            if (Language != null)
            {
                var hideLanguagesList = new Dictionary<string, Locale>();
                hideLanguagesList.Add(Language.Code, Language);
                fallBackComboBox.HideLanguagesList = hideLanguagesList;
            }
            fallBackComboBox.DataBind();
            if (!IsPostBack && Language != null)
            {
                fallbackLanguageLabel.Language = Language.Fallback;
                languageLanguageLabel.Language = Language.Code;
                languageComboBox.SetLanguage(Language.Code);
                fallBackComboBox.SetLanguage(Language.Fallback);
            }

            if (Language == null || Language.Code == PortalSettings.DefaultLanguage)
            {
                translatorsRow.Visible = false;
            }
            else
            {
                string defaultRoles = PortalController.GetPortalSetting(string.Format("DefaultTranslatorRoles-{0}", Language.Code), PortalId, "Administrators");

                translatorRoles.SelectedRoleNames = new ArrayList(defaultRoles.Split(';'));

                translatorsRow.Visible = true;
            }

            bool isEnabled = Null.NullBoolean;
            if (!IsAddMode)
            {
                Locale enabledLanguage = null;
                isEnabled = LocaleController.Instance.GetLocales(ModuleContext.PortalId).TryGetValue(Language.Code, out enabledLanguage);
            }

            cmdDelete.Visible = (UserInfo.IsSuperUser && !IsAddMode && !isEnabled && !Language.IsPublished && LocaleController.Instance.CanDeleteLanguage(Language.LanguageId) &&
                                 Language.Code.ToLowerInvariant() != "en-us");
        }

        private bool IsLanguageEnabled(string Code)
        {
            Locale enabledLanguage = null;
            return LocaleController.Instance.GetLocales(ModuleContext.PortalId).TryGetValue(Code, out enabledLanguage);
        }

        #endregion

        #region "Event Handlers"

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            cmdCancel.Click += cmdCancel_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdUpdate.Click += cmdUpdate_Click;
        }

        protected override void OnLoad(EventArgs e)
        {
            ClientAPI.AddButtonConfirm(cmdDelete, Localization.GetString("DeleteItem"));

            BindLanguage();
        }

        protected void cmdCancel_Click(object sender, EventArgs e)
        {
            try
            {
                Response.Redirect(Globals.NavigateURL(), true);
                //Module failed to load
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        protected void cmdDelete_Click(object sender, EventArgs e)
        {
            try
            {
                Localization.DeleteLanguage(Language);
                Response.Redirect(Globals.NavigateURL(), true);
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        protected void cmdUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (UserInfo.IsSuperUser)
                {
                    //Update Language
                    if (Language == null)
                    {
                        _Language = LocaleController.Instance.GetLocale(languageComboBox.SelectedValue);
                        if (_Language == null)
                        {
                            _Language = new Locale();
                            Language.Code = languageComboBox.SelectedValue;
                        }
                    }
                    Language.Fallback = fallBackComboBox.SelectedValue;
                    Language.Text = CultureInfo.CreateSpecificCulture(Language.Code).NativeName;
                    Localization.SaveLanguage(Language);
                }

                if (!IsLanguageEnabled(Language.Code))
                {
                    //Add language to portal
                    Localization.AddLanguageToPortal(PortalId, Language.LanguageId, true);
                }

                string roles = Null.NullString;
                if (IsAddMode)
                {
                    roles = string.Format("Administrators;{0}", string.Format("Translator ({0})", Language.Code));
                }
                else
                {
                    foreach (string role in translatorRoles.SelectedRoleNames)
                    {
                        roles += role + ";";
                    }

                    roles = roles.TrimEnd(';');
                }

                PortalController.UpdatePortalSetting(PortalId, string.Format("DefaultTranslatorRoles-{0}", Language.Code), roles);

                var tabCtrl = new TabController();
                TabCollection tabs = tabCtrl.GetTabsByPortal(PortalId).WithCulture(Language.Code, false);
                if (PortalSettings.ContentLocalizationEnabled && tabs.Count == 0)
                {
                    //Create Localized Pages
                    foreach (TabInfo t in tabCtrl.GetCultureTabList(PortalId))
                    {
                        tabCtrl.CreateLocalizedCopy(t, Language);
                    }

                    var portalCtl = new PortalController();
                    portalCtl.MapLocalizedSpecialPages(PortalId, Language.Code);
                }

                Response.Redirect(Globals.NavigateURL(), true);
                //Module failed to load
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion
    }
}