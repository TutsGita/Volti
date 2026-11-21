# SPDX-License-Identifier: AGPL-3.0-only
param([Parameter(Mandatory=$true)][string]$Captura)
$ErrorActionPreference='Stop'
$grupoAtual=$null
$porRelatorio=@{}
foreach($linha in Import-Csv -LiteralPath $Captura -Delimiter ([char]9)){
    if($linha.evento -eq 'marca'){
        $grupoAtual=$linha.dados
        continue
    }
    if($linha.evento -ne 'relatorio' -or !$grupoAtual -or $linha.dados -notmatch '^(?:[0-9A-F]{2} )*[0-9A-F]{2}$'){continue}
    $bytes=$linha.dados.Split(' ')
    $chave="$($linha.interface) / ID $($bytes[0]) / $($bytes.Length) bytes"
    if(!$porRelatorio.ContainsKey($chave)){$porRelatorio[$chave]=@{}}
    $grupos=$porRelatorio[$chave]
    if(!$grupos.ContainsKey($grupoAtual)){$grupos[$grupoAtual]=[Collections.Generic.List[object]]::new()}
    $grupos[$grupoAtual].Add($bytes)
}
$resultados=@(
    foreach($chave in $porRelatorio.Keys | Sort-Object){
        $grupos=$porRelatorio[$chave]
        if($grupos.Count -lt 2){continue}
        $nomes=@($grupos.Keys | Sort-Object)
        $tamanho=$grupos[$nomes[0]][0].Length
        for($i=1;$i -lt $tamanho;$i++){
            $valores=@(foreach($nome in $nomes){
                $amostras=$grupos[$nome]
                $unicos=@($amostras | ForEach-Object {$_[$i]} | Sort-Object -Unique)
                [pscustomobject]@{Marca=$nome; Valor=$unicos[0]; Estavel=$unicos.Count -eq 1; Amostras=$amostras.Count}
            })
            if(@($valores | Where-Object {!$_.Estavel}).Count -gt 0){continue}
            if(@($valores.Valor | Sort-Object -Unique).Count -lt 2){continue}
            $descricao=($valores | ForEach-Object {"$($_.Marca)=0x$($_.Valor) ($($_.Amostras)x)"}) -join '; '
            [pscustomobject]@{Relatorio=$chave; Byte=$i; Estados=$descricao}
        }
    }
)
if($resultados.Count -eq 0){
    Write-Output 'Nenhum byte estável variou entre as anotações para a mesma interface, ID e tamanho de relatório.'
}else{
    $resultados | Format-Table -AutoSize -Wrap
}
