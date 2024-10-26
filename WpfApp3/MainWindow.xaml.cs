using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfApp3;

namespace WpfApp3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(InputTextBox.Text, out int id))
            {
                using (var context = DbContext())
                {
                    var book = await context.Books
                        .FirstOrDefaultAsync(b => b.Id == id);

                    if (book != null)
                    {
                        OutputTextBlock.Text = FormatBookDetails(book);
                    }
                    else
                    {
                        OutputTextBlock.Text = "Book not found.";
                    }
                }
            }
            else
            {
                OutputTextBlock.Text = "Invalid ID.";
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(InputTextBox.Text, out int id))
            {
                using (var context = DbContext())
                {
                    var book = await context.Books.FindAsync(id);

                    if (book != null)
                    {
                        context.Books.Remove(book);
                        await context.SaveChangesAsync();
                        OutputTextBlock.Text = $"Book with ID {id} deleted.";
                    }
                    else
                    {
                        OutputTextBlock.Text = "Book not found.";
                    }
                }
            }
            else
            {
                OutputTextBlock.Text = "Invalid ID.";
            }
        }

        private string FormatBookDetails(Book book)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"ID: {book.Id}");
            builder.AppendLine($"Title: {book.Title}");
            builder.AppendLine($"Description: {book.Description}");
            builder.AppendLine($"Published On: {book.PublishedOn}");
            builder.AppendLine($"Price: {book.Price}");
            builder.AppendLine($"Publisher: {book.Publisher}");
            builder.AppendLine("Authors:");

            return builder.ToString();
        }

        public static ApplicationContext DbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseSqlServer("")
                .Options;
            return new ApplicationContext(options);
        }
    }
}

public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime PublishedOn { get; set; }
        public string? Publisher { get; set; }
        public decimal Price { get; set; }

        //Связи с другими классами
        public virtual ICollection<Author> Authors { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Category> Category { get; set; }
        public virtual ICollection<Promotion> Promotion { get; set; }
    }

    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }

        //Связи с другими классами
        public virtual ICollection<Book> Books { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Category> Categories { get; set; }


        public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>()
                        .HasMany<Author>(s => s.Authors)
                        .WithMany(c => c.Books)
                        .UsingEntity(e => e.ToTable("BookAuthor"));
        }
    }

    public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        private static IConfigurationRoot config;
        static ApplicationContextFactory()
        {
            // получаем конфигурацию из файла appsettings.json
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            config = builder.Build();
        }
        public ApplicationContext CreateDbContext(string[]? args = null)
        {
            // получаем строку подключения из файла appsettings.json
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            return new ApplicationContext(optionsBuilder.Options);
        }
    }

    public interface IAuthor
    {
        Task<IEnumerable<Author>> GetAllAuthorsAsync();
        Task<Author> GetAuthorWhithBooksAsync(int id);
        Task<Author> GetAuthorAsync(int id);
        Task<IEnumerable<Author>> GetAuthorsByNameAsync(string name);

        Task AddAuthorAsync(Author author);
        Task DeleteAuthorAsync(Author author);
        Task EditAuthorAsync(Author author);
    }

    public class AuthorRepository : IAuthor
    {
        public async Task AddAuthorAsync(Author author)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                await context.Authors.AddAsync(author);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteAuthorAsync(Author author)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                context.Authors.Remove(author);
                await context.SaveChangesAsync();
            }
        }

        public async Task EditAuthorAsync(Author author)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                context.Authors.Update(author);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Author>> GetAllAuthorsAsync()
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Authors.ToListAsync();
            }
        }

        public async Task<Author> GetAuthorWhithBooksAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Authors.Include(e => e.Books).FirstOrDefaultAsync(e => e.Id == id);
            }
        }

        public async Task<Author> GetAuthorAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Authors.FirstOrDefaultAsync(e => e.Id == id);
            }
        }

        public async Task<IEnumerable<Author>> GetAuthorsByNameAsync(string name)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Authors.Where(e => e.Name.Contains(name)).ToListAsync();
            }
        }
    }

    public class DbInit
    {
        public void Init(ApplicationContext context)
        {
            if (!context.Authors.Any())
            {
                context.Authors.AddRange(new Author[]
                {
                    new Author { Name = "Jess Kidd"},
                    new Author { Name = "Martha McPhee"},
                    new Author { Name = "Megan Miranda"},
                    new Author { Name = "Helen Phillips"},
                    new Author { Name = "Karen Kingsbury"}
                });
                context.SaveChanges();
            }
        }
    }

    public interface IBook
    {
        Task<IEnumerable<Book>> GetAllBooksAsync();
        Task<IEnumerable<Book>> GetAllBooksWithAuthorsAsync();

        Task<Book> GetBookAsync(int id);
        Task<IEnumerable<Book>> GetBooksByNameAsync(string name);
        Task<Book> GetBookWithPromotionAsync(int id);
        Task<Book> GetBookWithAuthorsAsync(int id);
        Task<Book> GetBookWithCategoryAndAuthorsAsync(int id);
        Task<Book> GetBookWithAuthorsAndReviewAsync(int id);
        Task<Book> GetBooksWithAuthorsAndReviewAndCategoryAsync(int id);

        Task AddBookAsync(Book book);
        Task DeleteBookAsync(Book book);
        Task EditBookAsync(Book book);
    }



    public class BookRepository : IBook
    {
        public async Task AddBookAsync(Book book)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                var authors = context.Authors.Where(e => book.Authors.Select(e => e.Id).Contains(e.Id));
                book.Authors = authors.ToList();
                await context.Books.AddAsync(book);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteBookAsync(Book book)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                context.Remove(book);
                await context.SaveChangesAsync();
            }
        }

        public async Task EditBookAsync(Book book)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                var currentBook = await context.Books.Include(e => e.Authors).FirstOrDefaultAsync(e => e.Id == book.Id);
                if (currentBook != null)
                {
                    currentBook.Title = book.Title;
                    currentBook.Description = book.Description;
                    currentBook.PublishedOn = book.PublishedOn;
                    currentBook.Price = book.Price;
                    currentBook.Authors = new List<Author>();

                    var authorsIds = book.Authors.Select(e => e.Id);
                    currentBook.Authors = await context.Authors.Where(e => authorsIds.Contains(e.Id)).ToListAsync();


                    context.Books.Update(currentBook);
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task<IEnumerable<Book>> GetAllBooksAsync()
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.ToListAsync();
            }
        }

        public async Task<IEnumerable<Book>> GetAllBooksWithAuthorsAsync()
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.Include(e => e.Authors).ToListAsync();
            }
        }

        public async Task<Book> GetBookAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.FirstOrDefaultAsync(e => e.Id == id);
            }
        }

        public async Task<IEnumerable<Book>> GetBooksByNameAsync(string name)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.Where(e => e.Title.Contains(name)).ToListAsync();
            }
        }

        public async Task<Book> GetBookWithAuthorsAndReviewAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.Include(e => e.Authors).Include(e => e.Reviews).FirstOrDefaultAsync(e => e.Id == id);
            }
        }

        public async Task<Book> GetBookWithAuthorsAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.Include(e => e.Authors).FirstOrDefaultAsync(e => e.Id == id);
            }
        }

        public async Task<Book> GetBookWithCategoryAndAuthorsAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.Include(e => e.Category).Include(e => e.Authors).FirstOrDefaultAsync(e => e.Id == id);
            }
        }

        public async Task<Book> GetBookWithPromotionAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.Include(e => e.Promotion).FirstOrDefaultAsync(e => e.Id == id);
            }
        }

        public async Task<Book> GetBooksWithAuthorsAndReviewAndCategoryAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Books.Include(e => e.Category).Include(e => e.Authors).Include(e => e.Reviews).FirstOrDefaultAsync(e => e.Id == id);
            }
        }
    }

    public class Review
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Comment { get; set; }
        public byte Stars { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; }
    }



    public interface IReview
    {
        Task<IEnumerable<Review>> GetAllReviewsAsync(int bookId);
        Task<Review> GetReviewAsync(int id);

        Task AddReviewAsync(Review review);
        Task DeleteReviewAsync(Review review);
    }


    public class ReviewRepository : IReview
    {
        public async Task AddReviewAsync(Review review)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                await context.Reviews.AddAsync(review);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteReviewAsync(Review review)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                context.Reviews.Remove(review);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Review>> GetAllReviewsAsync(int bookId)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Reviews.Where(e => e.BookId == bookId).ToListAsync();
            }
        }

        public async Task<Review> GetReviewAsync(int id)
        {
            using (ApplicationContext context = MainWindow.DbContext())
            {
                return await context.Reviews.FirstOrDefaultAsync(e => e.Id == id);
            }
        }
    }

    public class Promotion
    {
        public int Id { get; set; }
        public string Name { get; set; }
        //Возможно передача в виде процентах или конкретной суммы
        public decimal? Percent { get; set; }
        public decimal? Amount { get; set; }

        //Связи с другими классами
        public int BookId { get; set; }
        public Book? Book { get; set; }

        public override string ToString()
        {
            return String.Format("Name - {0}\nDiscount - {1}", Name, Percent ?? Amount);
        }
    }



public interface IPromotion
{
    Task<IEnumerable<Promotion>> GetAllPromotionsAsync();
    Task<Promotion> GetPromotionAsync(int id);

    Task AddPromotionAsync(Promotion promotion);
    Task EditPromotionAsync(Promotion promotion);
    Task DeletePromotionAsync(Promotion promotion);
}



public class PromotionRepository : IPromotion
{
    public async Task AddPromotionAsync(Promotion promotion)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            context.Promotions.Add(promotion);
            await context.SaveChangesAsync();
        }
    }
    public async Task EditPromotionAsync(Promotion promotion)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            context.Promotions.Update(promotion);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeletePromotionAsync(Promotion promotion)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            context.Promotions.Remove(promotion);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Promotion>> GetAllPromotionsAsync()
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            return await context.Promotions.ToListAsync();
        }
    }

    public async Task<Promotion> GetPromotionAsync(int id)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            return await context.Promotions.FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}



public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public virtual ICollection<Book> Books { get; set; }
}


public interface ICategory
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<IEnumerable<Category>> GetCategoriesByNameAsync(string name);
    Task<Category> GetCategoryAsync(int id);
    Task<Category> GetCategoryWithBooksAsync(int id);

    Task AddCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(Category category);
}



public class CategoryRepository : ICategory
{
    public async Task AddCategoryAsync(Category category)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            await context.Categories.AddAsync(category);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteCategoryAsync(Category category)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            context.Categories.Remove(category);
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            context.Categories.Update(category);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            return await context.Categories.ToListAsync();
        }
    }

    public async Task<Category> GetCategoryAsync(int id)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            return await context.Categories.FirstOrDefaultAsync(e => e.Id == id);
        }
    }

    public async Task<Category> GetCategoryWithBooksAsync(int id)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            return await context.Categories.Include(e => e.Books).FirstOrDefaultAsync(e => e.Id == id);
        }
    }

    public async Task<IEnumerable<Category>> GetCategoriesByNameAsync(string name)
    {
        using (ApplicationContext context = MainWindow.DbContext())
        {
            return await context.Categories.Where(e => e.Name.Contains(name)).ToListAsync();
        }
    }
}
