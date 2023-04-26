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
      const response = await axios.post("https://localhost:44323/api/UploadFile/upload", formData);
      console.log(response.data);
      alert("Archivo subido con éxito!");
    } catch (error) {
      console.error("Error al subir el archivo:", error);
      alert("Error al subir el archivo. Por favor, inténtalo de nuevo.");
    }
  };

  return (
    <div className="App">
      <h1>Subir Archivo</h1>
      <input type="file" onChange={handleFileChange} />
      <button onClick={handleFileUpload}>Subir Archivo</button>
    </div>
  );
}

export default App;
