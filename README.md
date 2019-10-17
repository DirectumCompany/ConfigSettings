# ConfigSettings

[![Build status](https://ci.appveyor.com/api/projects/status/rvtpa069lg82fshf/branch/master?svg=true)](https://ci.appveyor.com/project/hemnstill/configsettings/branch/master)
[![NuGet](https://img.shields.io/nuget/v/configsettings.svg)](https://www.nuget.org/packages/ConfigSettings)

**ConfigSettings** позволяет вынести настройки в отдельный файл и настроить их синхронизацию с Web.config или другими конфигурационными файлами.

## Быстрый старт 
* Подключить NuGet пакет:
 
   `Install-Package ConfigSettings`

* Создать файл `_ConfigSettings.xml` рядом с приложением: 
```xml
    <?xml version="1.0" encoding="utf-8"?>
    <settings>
      <var name="DATABASE_ENGINE" value="mssql" />
    </settings>
```

* Добавить в `Program.cs` код для чтения этой настройки:
```cs
    ConfigSettingsGetter configSettingsGetter = new ConfigSettingsGetter();  
    string dbEngine = configSettingsGetter.Get<string>("DATABASE_ENGINE");        
```      
    
Настройка `DATABASE_ENGINE` будет считана из файла `_ConfigSettings.xml` в переменную `dbEngine`.

## Возможности
* Чтение настроек из `xml` файла в определённую структуру. 
   * В будущем планируется добавить поддержку `json` формата.
* Живое отслеживание изменений в настройках.
* Слияние или импорт нескольких `_ConfigSettings.xml`:


  ```xml
       <import from="путь/до/другого/файла.xml">
  ``` 

* Поддержка нескольких типов настроек:
    
      Для простых типов, которые не нуждаются в сложной сериалазции, используется элемент:

     ```xml
       <var name="DATABASE_ENGINE" value="mssql" />
     ``` 

      Для сложных типов или для группировки списка настроек предлагается использовать блоки: 

     ```xml
       <block name="testBlockName">
         <tenant name="alpha" db="alpha_db" />
         <tenant name="beta" user="alpha_user" />
       </block>
     ```

* Автоматический поиск пути до файла с настройками. 

   * Если не передать путь к файлу явно, то будет использоваться первый попавшийся файл, заканчивающийся на `_configsettings.xml` без учёта регистра.
  
   * Поиск будет производится по всей иерархии папок вверх (примерно такой же алгоритм используется в `msbuild.exe` при поиске `Directory.Build.props`).

* Runtime изменение `App.config` или `Web.config` .
   
   * **Не работает под .NET Core**. Спасибо [stackoverflow.com](https://stackoverflow.com/questions/6150644/change-default-app-config-at-runtime)!
   
   * Пример использования:
  
      У нас есть конфиг приложения `Web.config`. Настройки из этого файла читаются напрямую third-party библиотекой, повлиять на поведение которой мы не можем. В runtime также задать настройки мы не можем. 
  
      Очевидным решением кажется необходимость задавать настройки напрямую в `Web.config`, но это может оказаться довольно неудобно. 
  
      Для упрощения модификации `Web.config` был реализован механизм синхронизации с настройками `_ConfigSettings.xml`.
  
      Например, можно добавить такой блок настроек с комментарием: 
     ```xml
     <hibernate-configuration xmlns="urn:nhibernate-configuration-2.2">
       <session-factory name="Default">
         <!--{@=CONNECTION_STRING}-->
         <property name="connection.connection_string"></property>
       </session-factory>
     </hibernate-configuration>
     ```
  
      Встроенный шаблонизатор подставит значение, заданное настройкой `CONNECTION_STRING` в файле `_ConfigSettings.xml`. В результате будет создан файл `Web.live.config` из которого third-party библиотека будет читать все настройки:
  
     ```xml
     <hibernate-configuration xmlns="urn:nhibernate-configuration-2.2">
         <session-factory name="Default">
           <!--{@=CONNECTION_STRING}-->
           <property name="connection.connection_string">Server=localhost;Database=db;User ID=postgres;Password=password</property>
         </session-factory>
       </hibernate-configuration>
     ``` 
  
      С полным перечнем возможностей шаблонизатора можно ознакомиться в тестах.
  
      Для активации механизма подмены пути до `Web.config` приложения и шаблонизации настроек, необходимо при инициализации приложения (в методе Main) добавить вызов:
     ```cs
     AppConfig.Change();
     ```

## Версионирование

Для версионирования используется [SemVer](http://semver.org/).  


## Лицензия

В этом проекте используется лицензия MIT.
Подробности в файле [LICENSE.md](LICENSE.md)
