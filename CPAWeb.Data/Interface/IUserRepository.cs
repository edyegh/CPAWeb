using System.Collections.Generic;
using System.Threading.Tasks;
using CPAWeb.Data.Model;

namespace CPAWeb.Data.Interface
{
    public interface IUserRepository
    {
        // Մուտքի ժամանակ՝ գտնում ենք օգտատիրոջը ամբողջական email-ով
        // կամ միայն '@'-ից առաջվա մասով (test -> test@gmail.com), ռեգիստրից անկախ:
        // Վերադարձնում է ցանկ, քանի որ մեկ օգտանունը կարող է համապատասխանել
        // մի քանի email-ի (test@gmail.com և test@yahoo.com):
        Task<List<AppUser>> FindByLoginAsync(string login);

        // "users" էջի ցանկը
        Task<List<AppUser>> GetAllAsync();

        // Ջնջելուց առաջ՝ ում ենք ջնջում
        Task<AppUser?> GetByIdAsync(long id);

        // Նոր օգտատիրոջ ավելացում, վերադարձնում է ստեղծված id-ն
        Task<long> CreateAsync(AppUser user);

        // Ջնջում, false՝ եթե տողն արդեն չկա
        Task<bool> DeleteAsync(long id);

        // Կրկնվող email-ի ստուգում՝ մինչև INSERT-ը
        Task<bool> EmailExistsAsync(string email);
    }
}
