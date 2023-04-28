import { useState } from 'react';
import './App.css';
import axios from 'axios';

function App() {
  const [selectedFile, setSelectedFile] = useState(null);

  const handleFileChange = (event) => {
    setSelectedFile(event.target.files[0]);
  };

  const handleFileUpload = async () => {
    if (!selectedFile) {
      alert("Por favor, seleccione un archivo antes de subirlo.");
      return;
    }

    const formData = new FormData();
    formData.append("file", selectedFile);

    try {
      const apiClient = axios.create({
        baseURL: "https://localhost:44323/api/UploadFile",
        headers: {
          "Content-Type": "multipart/form-data",
        },
        maxContentLength: Infinity,
        maxBodyLength: Infinity,
      });

      const response = await apiClient.post("/upload", formData);
      console.log(response.data);
      alert("Archivo subido con éxito!");
    } catch (error) {
      console.error("Error al subir el archivo:", error);
      alert("Error al subir el archivo. Por favor, inténtalo de nuevo.");
    }
  };

  // return (
  //     <div className="App">
  //       <h1>Subir Archivo</h1>
  //       <div className="file-upload-container">
  //         <input type="file" id="file" className="file-input" onChange={handleFileChange} />
  //         <label htmlFor="file" className="file-label">
  //           <span className="file-custom"></span>
  //         </label>
  //       </div>
  //       <button className="upload-button" onClick={handleFileUpload}>Subir Archivo</button>
  //     </div>
  //   );

  return (
    <div className="App">
      <h1>Subir Archivo</h1>
      <div className="file-upload-container">
        <input type="file" id="file" className="file-input" onChange={handleFileChange} />
        <label htmlFor="file" className="file-label">
          <span className="file-custom"></span>
        </label>
      </div>
      {selectedFile && <p className="file-name">{selectedFile.name}</p>}
      <button className="upload-button" onClick={handleFileUpload}>Subir Archivo</button>
    </div>
  );

  // <div className="App">
  //   <h1>Subir Archivo</h1>
  //   <input type="file" onChange={handleFileChange  } />
  //   <button onClick={handleFileUpload}>Subir Archivo</button>
  // </div>
  // );
}

export default App;
