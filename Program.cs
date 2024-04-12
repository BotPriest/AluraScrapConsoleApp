using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Newtonsoft.Json;

namespace AluraScrapConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var driver = new ChromeDriver())
            {
                driver.Navigate().GoToUrl("https://www.alura.com.br/busca?query=RPA");

                var buscaAluraPage = new BuscaAluraPage(driver);
                var cursos = buscaAluraPage.ObterCursos();

                var cursosJson = cursos.Select(curso => new CursoJson
                {
                    Href = curso.Href,
                    Titulo = curso.Titulo,
                    Descricao = curso.Descricao,
                    ValorDuracao = curso.ValorDuracao,
                    NomeInstrutor = curso.NomeInstrutor
                }).ToList();

                var cursoJsonDAO = new CursoJsonDAO("cursos_rpa.json");
                cursoJsonDAO.SalvarCursos(cursosJson);
            }
        }
    }

    public class CursoJson
    {
        public string Href { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string ValorDuracao { get; set; }
        public string NomeInstrutor { get; set; }
    }

    public static class WebElementExtensions
    {
        public static string TextContent(this IWebElement element)
        {
            return element.GetAttribute("textContent");
        }
    }

    public static class WebDriverExtensions
    {
        private static IWebDriver driver;

        public static IWebElement WaitUntilVisible(this WebDriver driver, By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(d => d.FindElement(locator).Displayed ? d.FindElement(locator) : null);
        }

        public static string GetTextContent(this IWebElement element, By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(d => d.FindElement(locator)).TextContent();
        }
    }

    public class CursoJsonDAO
    {
        private readonly string _filePath;

        public CursoJsonDAO(string filePath)
        {
            _filePath = filePath;
        }

        public void SalvarCursos(List<CursoJson> cursosJson)
        {
            if (cursosJson == null || cursosJson.Count == 0)
            {
                Console.WriteLine("No courses found to save to JSON file.");
                return;
            }

            try
            {
                var jsonSerializer = new JsonSerializer();

                using (var streamWriter = new StreamWriter(_filePath))
                {
                    using (var jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        jsonSerializer.Serialize(jsonWriter, cursosJson);
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error saving JSON file: " + ex.Message);
            }
        }
    }

    public class BuscaAluraPage
    {
        private readonly WebDriver _driver;

        public BuscaAluraPage(WebDriver driver)
        {
            _driver = driver;
        }

        public List<CursoJson> ObterCursos()
        {
            var cursos = new List<CursoJson>();

            try
            {
                var elementosCursos = _driver.FindElements(By.ClassName("busca-resultado"));

                foreach (var elementoCurso in elementosCursos)
                {
                    var curso = new CursoJson();

                    curso.Href = elementoCurso.FindElement(By.ClassName("busca-resultado-link")).GetAttribute("href");
                    curso.Titulo = elementoCurso.FindElement(By.ClassName("busca-resultado-nome")).TextContent();
                    curso.Descricao = elementoCurso.FindElement(By.ClassName("busca-resultado-descricao")).TextContent();

                    _driver.Navigate().GoToUrl(curso.Href);

                    curso.ValorDuracao = _driver.WaitUntilVisible(By.ClassName("formacao__info-destaque")).TextContent();
                    curso.NomeInstrutor = _driver.WaitUntilVisible(By.ClassName("formacao-instrutor-nome")).TextContent();

                    // Add the extracted course data to the list
                    cursos.Add(curso);
                }
            }
            catch (WebDriverException ex)
            {
                Console.WriteLine("Error extracting course data: " + ex.Message);
            }

            return cursos;
        }
    }
}

