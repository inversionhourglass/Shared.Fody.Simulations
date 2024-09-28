namespace Mono.Cecil
{
    public static class FieldReferenceExtensions
    {
        public static FieldDefinition ToDefinition(this FieldReference fieldRef)
        {
            if (fieldRef is not FieldDefinition fieldDef)
            {
                fieldDef = fieldRef.Resolve();
            }
            return fieldDef;
        }
    }
}
