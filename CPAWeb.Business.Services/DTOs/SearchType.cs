namespace CPAWeb.Services.DTOs
{
    // Որոնման տեսակը — UI-ի ցանկից ընտրվում է, թե որ query-ն է աշխատելու
    public enum SearchType
    {
        // cs.SERVICE_LOCATOR_VALUE (հին, լռելյայն որոնումը)
        Sid = 0,

        // cn.SERVICE_NAME — provider-ի համարով
        ProviderNumber = 1,

        // cp.NAME — provider-ի անունով (միայն cn.STATUS = 1)
        ProviderName = 2
    }
}
