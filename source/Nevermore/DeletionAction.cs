namespace Nevermore
{
    public enum DeletionAction
    {
        VetoDeletion, // User must delete the referencing documents manually
        SpecialAction, // We'll do something special for this rule 
        ImplicitlyDelete // The system will 'garbage collect' the referencing documents
    }
}