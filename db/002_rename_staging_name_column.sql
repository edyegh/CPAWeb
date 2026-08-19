/*
    Oracle. Ժամանակավոր աղյուսակի սյունակի վերանվանում.

      edyeghiazaryan_insertvalue.name  ->  edyeghiazaryan_insertvalue.locator_value

    Պատճառը՝ PL/SQL բլոկը (cpa_service_ident-ում գրանցումը) կարդում է
    locator_value սյունակը, և C#-ի կողմն էլ այժմ օգտագործում է նույն անունը
    (SIDRepository.StagingNameColumn).

    Կատարել մեկ անգամ, մինչև նոր տարբերակի գործարկումը.
*/

ALTER TABLE edyeghiazaryan_insertvalue RENAME COLUMN name TO locator_value;
