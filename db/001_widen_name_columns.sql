/*
    Անունների սյունակների ընդլայնում.

    Խնդիրը՝
      cpa_sid.Name                     varchar(11)  -> 11 նիշից երկար անունը INSERT-ի ժամանակ սխալ է տալիս
      edyeghiazaryan_insertvalue.name  varchar(20)  -> նույնը 20 նիշից հետո

    Բացի երկարությունից, varchar-ը SQL_Latin1_General collation-ով
    հայերեն տառեր չի պահում (դառնում են '?'), ուստի անցնում ենք nvarchar-ի.

    Սյունակների վրա index կամ constraint չկա (միակ PK-ն cpa_sid.id-ի վրա է),
    ուստի ALTER-ը անվտանգ է և տվյալներ չի կորցնում.
    ՈՒՇԱԴՐՈՒԹՅՈՒՆ. արդեն կտրված (11 նիշի) տողերը հետ չեն վերականգնվում.
*/

ALTER TABLE dbo.cpa_sid                    ALTER COLUMN Name nvarchar(200) NULL;
GO

ALTER TABLE dbo.edyeghiazaryan_insertvalue ALTER COLUMN name nvarchar(200) NULL;
GO
