# Stinger Toolkit

Ferramenta de diagnóstico para Windows e para o receptor USB Kingston **0951:16DA** do HyperX Cloud Stinger Wireless de PS4, modelo **HX-HSCSW-BK**. Ela registra relatórios HID de entrada em hexadecimal. A finalidade é descobrir o formato da bateria para que você possa implementar seu próprio indicador.

## Como coletar

1. Extraia o ZIP, conecte o receptor e abra **StingerToolkit.exe** em um terminal.
2. Pressione **Enter** e anote o estado que vai observar. Comece, por exemplo, com **ligado** e então ligue o fone. Aguarde alguns segundos.
3. Marque **carregando** antes de conectar o cabo. Marque **fora do cabo** antes de retirá-lo. Faça capturas em níveis diferentes, anotando porcentagens apenas quando houver uma referência confiável. Repita ações como mudar volume em um bloco próprio para ajudar a descartar bytes que pertencem a outros eventos.
4. Pressione **Esc** para encerrar. O arquivo **stinger-*.tsv** fica em **Documentos**. A opção **--list** conta interfaces do receptor sem iniciar a captura.

Cada linha descreve um relatório, uma anotação, os dados de uma interface ou um erro. Os tempos são em milissegundos a partir do início. As interfaces aparecem como **hid1**, **hid2** etc. Use os eventos **interface** para identificar o uso HID. O TSV pode ser aberto em uma planilha.

## Como investigar um byte candidato

No PowerShell, na pasta extraída, execute o comando abaixo com o caminho do seu arquivo.

```powershell
.\Comparar.ps1 -Captura 'C:\caminho\stinger-20261120-200000-000.tsv'
```

O script agrupa relatórios pela interface, primeiro byte e tamanho. Ele lista bytes que se mantêm estáveis dentro de cada estado anotado e mudam entre estados. A numeração começa em zero. Os resultados são candidatos. Compare com níveis conhecidos e com o fone fora do carregador para distinguir bateria, estado de carga e outros campos.

## Quando a captura passiva fica vazia

O toolkit abre o receptor para leitura. Se a bateria só vier em resposta a uma consulta, capture a consulta e a resposta com [Wireshark e USBPcap](https://wiki.wireshark.org/CaptureSetup/USB) enquanto um aplicativo compatível realmente exibe a bateria do fone. Identifique o receptor pelo **0951:16DA** e filtre a captura ao endereço USB desse dispositivo. O Wireshark pode capturar tráfego de outros periféricos no mesmo barramento. Confira o conteúdo antes de compartilhar.

O código está sob AGPL-3.0-only. Os avisos de terceiros acompanham o pacote. Para compilar a partir do ZIP de fontes, execute **tools/StingerToolkit/Build.ps1** no Windows.
