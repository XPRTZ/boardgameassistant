import { HttpClient, HttpClientModule } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';

import { ChromaClient, Collection } from "chromadb";

import { MarkdownNodeParser } from "@llamaindex/core/node-parser";
import { Document } from "@llamaindex/core/schema";
import { Observable, range } from 'rxjs';
import { CommonModule } from '@angular/common';


interface Message {

  content: string;

  speaker: "User" | "Assistant";

}
 
  


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HttpClientModule, CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  messages: Message[] = [];
  prompt: string = '';
  private httpClient: HttpClient;

  constructor(http: HttpClient) {
    this.httpClient = http;
  }
  
  title = 'BoardGameAssistant';

  collection?: Collection;

  async ngOnInit() {

    const client = new ChromaClient({ path: "http://localhost:8000" })
    client.heartbeat()
        
    /*if (await client.countCollections() > 0 ) {
      await client.deleteCollection({ name: "boardgame_rules" })
    }*/

    this.collection = await client.getOrCreateCollection({
      name: "boardgame_rules",
    });

    console.log({items: await this.collection.count()})

//    const ruleBooks = ['wm_rulebook_v29', 'Inventions Rules Public', 'Kanban_EV_Rules_v0.99', 'Lisboa_rulebook_final', 'On_Mars_-_English_Rulebook_FINAL', 'Speakeasy Rules v16'];
//const ruleBooks = [];
    const ruleBooks = ['On_Mars_-_English_Rulebook_FINAL', 'wm_rulebook_v29'];

    const collectionPromises = [].map(ruleBook => {
    this.httpClient.get(`/Rulebooks/${ruleBook}.md`, {responseType: 'text'})
        .subscribe(async text => {
          const splitter = new MarkdownNodeParser();
          const nodes = splitter.getNodesFromDocuments([new Document({ text })]);
          const chunks = nodes.map(node => node.text);
          const ids = nodes.map(node => node.id_);
          const metadatas = nodes.map(_ => ({ ruleBook}));

          console.log({chunks, ids, metadatas})

          return this.collection!.add({
            documents: chunks,
            ids: ids,
            metadatas: metadatas,
          });
        });      
    });   

    await Promise.all(collectionPromises);   
    
  }

  async chat() {

    this.messages.push({ content: this.prompt, speaker: "User" });

    (await this.callApi(this.prompt)).subscribe(response => {

      this.messages.push({ content: (response as any).answer, speaker: "Assistant" });
    });    

  }

  async callApi(prompt: string): Promise<Observable<Object>> {
    
    // const prompt = "Which game features the professor LATIV?";

    const results = await this.collection!.query({
      queryTexts: prompt, // Chroma will embed this for you
      nResults: 5, // how many results to return
    });
    
    console.log({results});

    results.documents.map(d => ({ content: d}))

    console.log({items: await this.collection!.count()})

    // const params = [results.documents.map(d => ({content: d})), results.metadatas.map(m => ({ruleBook: m, url: `http://localhost:4200/Rulebooks/${m}.md`}))]

    let params: any[] = []
    console.log({documents: results.documents})
    results.documents[0].forEach((document,index): void => {
      const ruleBook = results.metadatas[0][index]!['ruleBook'];
      params[index] = { content: document, ruleBook, url: `http://localhost:4200/Rulebooks/${ruleBook}.md` }
    })
    console.log({params})

    return this.httpClient.post(`https://localhost:44310/assistant/${prompt}`, params);
  }
}
