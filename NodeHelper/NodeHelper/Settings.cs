namespace NodeHelper
{
    public class NodeHelper : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI ("Use Blizzy Toolbar")]
        public bool _blizzyToolbar = false;

        public override string DisplaySection
        {
            get
            {
                return "Node Helper";
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }

        public override string Section
        {
            get
            {
                return "Editor Settings";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override string Title
        {
            get
            {
                return "Node Helper Settings";
            }
        }
    }
}
