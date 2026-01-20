namespace GoogleSheetImporter.Mappers
{
    public class CommonConfigMapper : IConfigMapper
    {
        private readonly IConfigTokenProvider _tokenProvider;
        private readonly IConfigPropertySetter _setter;
        private readonly IConfigModifier[] _modifiers;

        public CommonConfigMapper(
            IConfigTokenProvider tokenProvider,
            IConfigPropertySetter setter,
            params IConfigModifier[] modifiers)
        {
            _tokenProvider = tokenProvider;
            _setter = setter;
            _modifiers = modifiers;
        }

        public void Apply()
        {
            var token = _tokenProvider.GetToken();
            foreach (var modifier in _modifiers)
            {
                modifier.Modify(token);
            }

            _setter.Apply(token);
        }
    }
}
