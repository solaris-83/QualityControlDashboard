 const Style = {
  info: [
    "color: #ffffff",
    "font-weight: bold",
    "font-size: 18px",
    "background-color: rgb(10, 128, 246)",
    "padding: 2px 4px",
    "border-radius: 2px"
  ],
  error: [
    "color: #eee",
    "background-color: red"
  ],
  success: [
    "background-color: green"
  ]
}

const log = (text : any, extra :string[]) => {
  let style = Style.info.join(';') + ';';
  style += extra.join(';'); // Add any additional styles
  console.log(`%c${text}`, style);
}

export const logError = (text : any) => {
  log(text, Style.error)
}   

export const logInfo = (text : any) => {
  log(text, Style.info)
}  

export const logSuccess = (text : any) => {
  log(text, Style.success)
}   