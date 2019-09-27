# ConfigSettings

[![Build status](https://ci.appveyor.com/api/projects/status/rvtpa069lg82fshf/branch/master?svg=true)](https://ci.appveyor.com/project/hemnstill/configsettings/branch/master)
[![NuGet](https://img.shields.io/nuget/v/configsettings.svg)](https://www.nuget.org/packages/ConfigSettings)

Чтение и запись настроек .net приложения. 

Когда формат файла App.config кажется слишком избыточным. 
Позволяет все настройки вынести в отдельный файл. 

## Быстрый старт 
* подключить NuGet пакет:
 
   `Install-Package ConfigSettings`

* создать файл `_ConfigSettings.xml` рядом с приложением: 
    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <settings>
      <var name="DATABASE_ENGINE" value="mssql" />
    </settings>
    ```

* добавить в `Program.cs` код для чтения этой настройки:
    ```
    var configSettingsGetter = new ConfigSettingsGetter();  
    var dbEngine = configSettingsGetter.Get<string>("DATABASE_ENGINE");        
    ```      
    
    Настройка `DATABASE_ENGINE` будет считана из файла `_ConfigSettings.xml` в переменную `dbEngine`.

## Возможности
* чтение настроек из `xml` файла определённую структуру. Для этого используется класс `ConfigSettingsGetter`. В будущем планируется добавить поддержку `json` формата.
* отслеживание изменений при чтении настроек. Для этого необходимо использовать класс `ReloadedConfigSettingsGetter`
* можно импортировать настройки из другого файла. Для этого используется конструкция `<import from="путь/до/другого/файла.xml">`.  
* существует 2 вида настроек. 
    
    Для строковых значений, которые не нуждаются в сложной сериалазции, используется элемент:
  `<var name="DATABASE_ENGINE" value="mssql" />`. 
  
  при чтении используется метод:  
  ```ConfigSettingsGetter.Get<T>(string name, T defaultValue)```

  Для сложных типов или для группировки списка настроек предлагается использовать блоки: 
     ```
    <block name="testBlockName">
       <tenant name="alpha" db="alpha_db" />
       <tenant name="beta" user="alpha_user" />
     </block>
    ```
    при чтении блоков используется метод: ```ConfigSettingsGetter.GetBlock<T>(string name)```.  

* есть возможность менять содержимое `App.config` или `Web.config` файла в runtime (не работает под .net core). Спасибо [stackoverflow.com](https://stackoverflow.com/questions/6150644/change-default-app-config-at-runtime)!
  
  Рассмотрим такой пример: 
  
  У нас есть конфиг приложения `Web.config`. Настройки из этого файла читаются напрямую third-party библиотекой, повлиять на поведение которой мы не можем. 
  И в runtime задать настройки тоже не можем. 
  
  Получается, нам необходимо задавать их прямо в `Web.config`, но это может быть не очень удобно. 
  
  Для вынесения настроек в отдельный файл был придуман механизм, который умеет менять содержимое `Web.config` в соответствии с нашими настройками. 
  Например, можно добавить такой блок настроек с комментарием: 
  ```xml
  <hibernate-configuration xmlns="urn:nhibernate-configuration-2.2">
    <session-factory name="Default">
      <!--{@=CONNECTION_STRING}-->
      <property name="connection.connection_string"></property>
    </session-factory>
  </hibernate-configuration>
  ```
  
  Встроенный шаблонизатор подставит значение, заданное в настройке `CONNECTION_STRING`, из файла `_ConfigSettings.xml`. При этом будет создан файл `Web.live.config` из которого third-party библиотека будет читать все настройки:
  ```xml
  <hibernate-configuration xmlns="urn:nhibernate-configuration-2.2">
      <session-factory name="Default">
        <!--{@=CONNECTION_STRING}-->
        <property name="connection.connection_string">Server=localhost;Database=db;User ID=postgres;Password=password;Port=5433;Client Encoding=UTF8</property>
      </session-factory>
    </hibernate-configuration>
  ``` 
  
  Поддерживается довольно ограниченный синтаксис. Полный перечень примеров можно посмотреть в тестах.
  
  Чтобы воспользоваться механизмом, который подменяет путь до `Web.config` приложения и проставляет значения настроек, необходимо в самом начале (в методе Main) добавить вызов:
  ```
  AppConfig.Change();
  ```
  
* поиск пути до файла с настройками. 
 
  Если не передать путь к файлу явно, то будет использоваться первый попавшийся файл, заканчивающийся на `_configsettings.xml` без учёта регистра.
  
  Поиск будет производится по всей иерархии папок вверх 
  (примерно такой же алгоритм используется в `msbuild.exe` при поиске `Directory.Build.props`).
  

## Версионирование

Для версионирования используется [SemVer](http://semver.org/).  


## Лицензия

В этом проекте используется лицензия MIT - подробности в файле [LICENSE.md](LICENSE.md)