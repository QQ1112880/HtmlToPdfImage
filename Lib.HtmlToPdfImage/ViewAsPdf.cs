﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lib.HtmlToPdfImage
{
    public class ViewAsPdf : AsPdfResultBase
    {
        private string _viewName;

        public new string ViewName
        {
            get { return _viewName ?? string.Empty; }
            set { _viewName = value; }
        }

        private string _masterName;

        public string MasterName
        {
            get { return _masterName ?? string.Empty; }
            set { _masterName = value; }
        }

        public new object Model { get; set; }

        public new ViewDataDictionary ViewData { get; set; }

        public ViewAsPdf(ViewDataDictionary viewData = null)
        {
            this.WkhtmlPath = string.Empty;
            MasterName = string.Empty;
            ViewName = string.Empty;
            Model = null;
            ViewData = viewData;
        }

        public ViewAsPdf(string viewName, ViewDataDictionary viewData = null)
            : this(viewData)
        {
            ViewName = viewName;
        }

        public ViewAsPdf(object model, ViewDataDictionary viewData = null)
            : this(viewData)
        {
            Model = model;
        }

        public ViewAsPdf(string viewName, object model, ViewDataDictionary viewData = null)
            : this(viewData)
        {
            ViewName = viewName;
            Model = model;
        }

        public ViewAsPdf(string viewName, string masterName, object model)
            : this(viewName, model)
        {
            MasterName = masterName;
        }

        protected override string GetUrl(ActionContext context)
        {
            return string.Empty;
        }

        protected virtual ViewEngineResult GetView(ActionContext context, string viewName, string masterName)
        {
            var engine = context.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
            return engine.FindView(context, viewName, true);
        }

        protected override async Task<byte[]> CallTheDriver(ActionContext context)
        {
            // use action name if the view name was not provided
            string viewName = ViewName;
            if (string.IsNullOrEmpty(ViewName))
            {
                viewName = ((Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor).ActionName;
            }

            ViewEngineResult viewResult = GetView(context, viewName, MasterName);
            var html = new StringBuilder();

            //string html = context.GetHtmlFromView(viewResult, viewName, Model);
            ITempDataProvider tempDataProvider = context.HttpContext.RequestServices.GetService(typeof(ITempDataProvider)) as ITempDataProvider;

            var viewDataDictionary = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: new ModelStateDictionary())
            {
                Model = this.Model
            };
            if (this.ViewData != null)
            {
                foreach (var item in this.ViewData)
                {
                    viewDataDictionary.Add(item);
                }
            }
            using (var output = new StringWriter())
            {
                var view = viewResult.View;
                var tempDataDictionary = new TempDataDictionary(context.HttpContext, tempDataProvider);
                var viewContext = new ViewContext(
                    context,
                    viewResult.View,
                    viewDataDictionary,
                    tempDataDictionary,
                    output,
                    new HtmlHelperOptions());
                
                await view.RenderAsync(viewContext);

                html = output.GetStringBuilder();
            }


            string baseUrl = string.Format("{0}://{1}", context.HttpContext.Request.Scheme, context.HttpContext.Request.Host);
            var htmlForWkhtml = Regex.Replace(html.ToString(), "<head>", string.Format("<head><base href=\"{0}\" />", baseUrl), RegexOptions.IgnoreCase);

            byte[] fileContent = WkhtmltopdfDriver.ConvertHtml(this.WkhtmlPath, this.GetConvertOptions(), htmlForWkhtml);
            return fileContent;
        }
    }
}