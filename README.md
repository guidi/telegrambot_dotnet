# TelegramBotCSharp
Bot de controle de computador usando API do Telegram.

A API de bots do telegram é extremamente útil e fácil de utilizar, neste programa de exemplo eu a utilizo para controlar remotamente um ou vários computadores que estejam executando o programa.

Como utilizar:

Primeiro de tudo é necessário criar um bot e um token de acesso para ele no telegram, isso pode ser feito através do botfather, não vou explicar o processo pois já tem a explicação em vários lugares na internet.

https://core.telegram.org/bots#creating-a-new-bot

Depois disso vá para o código fonte e no MainForm altere o valor da variável botToken para o token do seu bot, depois disso quando você mandar mensagem para o bot o evento BotOnMessageReceived vai ser chamado, é nesse evento que a mágica acontece.

A varíavel AuthorizedID deve ser preenchida com o seu id do telegram para que outra pessoa não consiga enviar comandos para o seu bot, para descobrir seu id basta enviar uma mensagem para o bot e em modo debug verificar o valor existente em message.From.Id.

Com o bot rodando você pode enviar alguns comandos para ele, lembrando que o programa rodando o bot pode ser instalado em um ou vário computadores.

Comandos:

/list - Lista o id de todos os computadores que estão com o programa do bot rodando.
![print](https://guidi.io/img_externa/bot_telegram_list.jpg)

Com o id do bot que basicamente é um concatenação de nome da máquina + usuário logado você pode mandar comandos específicos para uma máquina, como por exemplo.

/idmaquina #info - Lista as informações da máquina.
![print](https://guidi.io/img_externa/bot_telegram_info.jpg)


Tem vários outros comandos como:

Tirar screeenshot, enviar um arquivo da máquina para o telegram, executar um comando no CMD (prompt do DOS), executar um arquivo de música, abrir o navegador dentre outros, deixa de ser preguiçoso e dá uma olhadinha no código.

Lembrando que este programa foi desenvolvido para FINS DE ESTUDO, então vê se não vai fazer merdinha com a máquina dos outros.
