using Discord.Commands;
using MisfitBot2.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Modules
{
    class PluginTemplateModule : ModuleBase<ICommandContext>
    {
        private readonly PluginTemplateService _service;

        // Remember to add an instance of the AudioService
        // to your IServiceCollection when you initialize your bot
        public PluginTemplateModule(PluginTemplateService service)
        {
            _service = service;
        }
    
    }
}
