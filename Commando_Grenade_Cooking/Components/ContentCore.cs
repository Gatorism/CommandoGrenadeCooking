using RoR2.ContentManagement;

namespace Commando_Grenade_Cooking
{
    public class ContentCore
    {
        public static bool initialized = false;
        public static void Init()
        {
            if (initialized) return;
            ContentManager.collectContentPackProviders += ContentCore.ContentManager_collectContentPackProviders;
        }

        private static void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new Content());
        }
    }
}