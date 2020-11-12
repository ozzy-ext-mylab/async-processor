# MyLab.AsyncProcessor
SDK: [![NuGet Version and Downloads count](https://buildstats.info/nuget/MyLab.AsyncProcessor.Sdk)](https://www.nuget.org/packages/MyLab.AsyncProcessor.Sdk)

Docker образ: [![Docker image](https://img.shields.io/docker/v/ozzyext/mylab-async-proc-api?sort=semver)](https://hub.docker.com/r/ozzyext/mylab-async-proc-api)

Спецификация API: [![API specification](https://img.shields.io/badge/OAS3-oas3%20specifiecation-green)](https://app.swaggerhub.com/apis/ozzy/my-lab_async_processor_api/1)

```
Поддерживаемые платформы: .NET Core 3.1+
```
Ознакомьтесь с последними изменениями в [журнале изменений](/changelog.md).

## Обзор

`MyLab.AsyncProcessor` - платформа для разработки асинхронных обработчиков запросов с использованием микросервисной архитектуры.

![mylab-asyncproc-simple](./doc/diagramms/mylab-asyncproc-simple.png)

Архитектура асинхронного процессора подразумевает, что:

* взаимодействие происходит через `API`
* обработка запроса происходит в обработчике запросов бизнес-логики `BizProc`
* `BizProc`получает запросы по событийной модели
* `API` и `BizProc`могут быть масштабированы 

На диаграмме ниже изображено взаимодествие клиента с асинхронным процессором через `API`:

![](./doc/diagramms/mylab-asyncproc-client-sequence.png)

## Внутри

![](./doc/diagramms/mylab-asyncproc-struct.png)

Основные компоненты асинхронного процессора:

* `API` - обрабатывает входящие служебные запросы для подачи бизнес-запроса на обработку, получения статуса обработки, а также для получения результатов обработки;
* `BizProc` - бизнес-процессор. Выполняет обрабтку поступившего бизнес-запроса в соответствии со специической логикой;
* `Redis` - используется для временного хранения состояния выполнения запроса и информации о результате его выполнения;
* `RabbitMQ` - используется для организации событийной модели при передаче запросов от `API` к `BizProc`.

Особенности взаимодействия компонентов:

* `API` передаёт бизнес-запрос на обработку через очередь;
* `BizProc` сообщает об изменении состояния обработки запроса напряму в `API`;
* `API` хранит состояние обработки запроса и результаты в `Redis`;
* обработка бизнес-запроса не обязательно должна выполняться в один поток в пределах `BizProc`. Но `BizProc`  ответственнен за начало этой обработки.

## Жизнь запроса

### Жизненный цикл

![](./doc/diagramms/mylab-asyncproc-request-life.png)

### Время жизни запроса

В настройках `API` предусмотрено два параметра, влияющих на время жизни запроса:

* `MaxIdleTime` - период жизни запроса, начиная с последней активности. Обновляется после создания запроса и обновлении его статуса.
* `MaxStoreTime` - период жизни запроса после завершения обработки. Обновляется при переходе в состояние `Completed`.

По истечению времени жизни, информация о запросе удяляется из `Redis` и `API` на все запросы по нему отвечает `404`.

## Статус запроса

### Содержание статуса

Статус запроса содержит следующие параметры:

* `processStep` - этап жизни запроса:
  * `pending`
  * `processing`
  * `completed`
* `bizStep` - этап обработки в терминах бизнес-процесса. Перечень значений зависит от конуретной предметной области;
* `successful`- флаг, указывающий фактор успешности обработки зпроса на шаге `Completed`. Если `false` - в процессе обработки, возникла ошибка, при этом должно быть заполнено поле `error`;
* `error` - содержит информацию об ошибке, возникшей в процессе обработки запроса;
  * `bizMgs` - сообщение бизнес-уровня, понятное пользователю;
  * `techMgs` - техническое сообщение. Обычно, сообщение из исключения;
  * `techInfo` - развёрнутое техническое описание ошибки. Обычно - stacktrace исключения;
* `resultSize` - размер результата обработки запроса;
* `resultMime` - `MIME` тип результата обработки запроса.

Подробнее - [тут](https://app.swaggerhub.com/apis/ozzy/my-lab_async_processor_api/1#/RequestStatus).

### Бизнес-шаг обработки (BizStep)

`BizStep` пердназначен для того, чтобы детализировать этапы обработки запроса, специфичные для конкретной задачи. Эти шаги устанавливаются только в процессе обработки запроса, т.е. в то время, когда `processStep == processing`.

![](./doc/diagramms/mylab-asyncproc-biz-step.png)

## Конфигурирование API

`API` поддерживает [удалённый конфиг](https://github.com/ozzy-ext-mylab/remote-config#%D0%BA%D0%BE%D0%BD%D1%84%D0%B8%D0%B3%D1%83%D1%80%D0%B8%D1%80%D0%BE%D0%B2%D0%B0%D0%BD%D0%B8%D0%B5).

Имя узла конфигурации - `AsyncProc`. Ниже приведён пример конфигурации:

```json

```

